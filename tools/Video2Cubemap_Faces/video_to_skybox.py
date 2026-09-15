"""Local rotating-video stitcher and explicit-coverage equirectangular/cubemap exporter."""
import argparse
import datetime as dt
import json
import math
from pathlib import Path
import sys
import traceback

try:
    import cv2
    import numpy as np
except ImportError:
    print('Missing dependencies. Run Setup.bat first.', file=sys.stderr)
    raise SystemExit(2)


def save(path, array):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    ok, data = cv2.imencode(path.suffix, array)
    if not ok:
        raise RuntimeError(f'Image encoding failed: {path}')
    data.tofile(str(path))  # Unicode Windows paths supported.


def extract(video, count, max_width, out):
    cap = cv2.VideoCapture(str(video))
    if not cap.isOpened():
        raise ValueError('Cannot open video. Try a local H.264 MP4 file.')
    total = int(cap.get(cv2.CAP_PROP_FRAME_COUNT))
    fps = float(cap.get(cv2.CAP_PROP_FPS))
    if total < 2 or fps <= 0:
        cap.release()
        raise ValueError('Video has no usable frame count/FPS.')
    indices = np.unique(np.linspace(0, total - 1, min(count, total)).astype(int))
    frames, measurements = [], []
    try:
        for idx in indices:
            cap.set(cv2.CAP_PROP_POS_FRAMES, int(idx))
            ok, img = cap.read()
            if not ok:
                continue
            if img.shape[1] > max_width:
                img = cv2.resize(img, (max_width, round(img.shape[0] * max_width / img.shape[1])), interpolation=cv2.INTER_AREA)
            sharpness = float(cv2.Laplacian(cv2.cvtColor(img, cv2.COLOR_BGR2GRAY), cv2.CV_64F).var())
            name = f'frame_{int(idx):07d}.jpg'
            save(out / name, img)
            frames.append(img)
            measurements.append({'frame': int(idx), 'seconds': round(int(idx)/fps, 3), 'sharpness': sharpness, 'file': name})
    finally:
        cap.release()
    return frames, {'fps': fps, 'frames_total': total, 'duration_seconds': total / fps, 'samples': measurements}


def spherical_fallback(frames):
    """SIFT + reprojection bundle adjustment for cases rejected by the default ORB pipeline."""
    features=[cv2.detail.computeImageFeatures2(cv2.SIFT_create(nfeatures=3000), im) for im in frames]
    matcher=cv2.detail.BestOf2NearestMatcher_create(False, .65)
    matches=matcher.apply2(features);matcher.collectGarbage()
    indices=cv2.detail.leaveBiggestComponent(features,matches,.6)
    if len(indices)<3:raise ValueError('SIFT found fewer than three connected views')
    selected=[frames[int(i)] for i in indices]
    if len(selected)!=len(frames):
        features=[cv2.detail.computeImageFeatures2(cv2.SIFT_create(nfeatures=3000), im) for im in selected]
        matcher=cv2.detail.BestOf2NearestMatcher_create(False,.65)
        matches=matcher.apply2(features);matcher.collectGarbage()
    ok,cameras=cv2.detail_HomographyBasedEstimator().apply(features,matches,None)
    if not ok:raise ValueError('SIFT camera estimation failed')
    for camera in cameras:camera.R=camera.R.astype(np.float32)
    adjust=cv2.detail_BundleAdjusterReproj();adjust.setConfThresh(.6)
    refine=np.zeros((3,3),np.uint8);refine[0,0]=1;adjust.setRefinementMask(refine)
    ok,cameras=adjust.apply(features,matches,cameras)
    if not ok:raise ValueError('SIFT camera refinement also failed')
    focal=float(np.median([c.focal for c in cameras]))
    if not 50<focal<10000:raise ValueError('Unreliable focal estimate')
    rotations=cv2.detail.waveCorrect([c.R for c in cameras],cv2.detail.WAVE_CORRECT_HORIZ)
    warper=cv2.PyRotationWarper('spherical',focal)
    warped=[]
    for im,c,r in zip(selected,cameras,rotations):
        k=c.K().astype(np.float32)
        roi=warper.warpRoi((im.shape[1],im.shape[0]),k,r)
        if roi[2]*roi[3]>30000000:raise ValueError('Unreliable warped image size')
        corner,wi=warper.warp(im,k,r,cv2.INTER_LINEAR,cv2.BORDER_CONSTANT)
        _,wm=warper.warp(np.full(im.shape[:2],255,np.uint8),k,r,cv2.INTER_NEAREST,cv2.BORDER_CONSTANT)
        warped.append((corner,wi,wm))
    x0=min(c[0] for c,_,_ in warped);y0=min(c[1] for c,_,_ in warped)
    x1=max(c[0]+im.shape[1] for c,im,_ in warped);y1=max(c[1]+im.shape[0] for c,im,_ in warped)
    w,h=x1-x0,y1-y0
    if w*h>30000000:raise ValueError('Unreliable panorama bounds')
    acc=np.zeros((h,w,3),np.float32);weights=np.zeros((h,w),np.float32)
    for (x,y),im,mask in warped:
        weight=np.minimum(cv2.distanceTransform(mask,cv2.DIST_L2,3),32)/32
        yy,xx=y-y0,x-x0;hh,ww=mask.shape
        acc[yy:yy+hh,xx:xx+ww]+=im.astype(np.float32)*weight[:,:,None]
        weights[yy:yy+hh,xx:xx+ww]+=weight
    return np.uint8(np.clip(acc/np.maximum(weights[:,:,None],1e-6),0,255)),np.uint8(weights>0)*255


def stitch(frames):
    if len(frames) < 3:
        raise ValueError('Need at least three readable overlapping frames.')
    # PANORAMA uses a spherical warper. Its cropped output is NOT itself a full sphere.
    engine = cv2.Stitcher_create(cv2.Stitcher_PANORAMA)
    engine.setPanoConfidenceThresh(0.65)
    status, pano = engine.stitch(frames)
    errors = {1: 'Not enough matching frames', 2: 'Homography estimation failed', 3: 'Camera refinement failed'}
    if status != cv2.Stitcher_OK or pano is None:
        print(f'Default stitcher failed ({errors.get(status,status)}); trying SIFT spherical refinement...',flush=True)
        try:return spherical_fallback(frames)
        except Exception as exc:raise ValueError(f'Stitch failed ({status}); fallback: {exc}. Sample frames retained.') from exc
    mask = engine.resultMask() if hasattr(engine, 'resultMask') else None
    if mask is None or mask.shape != pano.shape[:2]:
        # Explicit conservative fallback; black source regions are treated as missing.
        mask = (np.max(pano, axis=2) > 0).astype(np.uint8) * 255
    return pano, mask


def place_panorama(pano, mask, width, coverage, horizon, fill):
    """Map the spherical strip using user supplied horizontal sweep; vertical scale is inferred.

    This cannot recover unknown FOV, camera tilt, missing directions or true HDR radiance.
    """
    height = width // 2
    ph, pw = pano.shape[:2]
    radians_per_pixel = math.radians(coverage) / pw
    lon = ((np.arange(width, dtype=np.float32) + .5) / width - .5) * (2 * np.pi)
    lat = (.5 - (np.arange(height, dtype=np.float32) + .5) / height) * np.pi
    mx = np.broadcast_to(lon[None, :] / radians_per_pixel + pw / 2 - .5, (height, width)).copy()
    my = np.broadcast_to(horizon * ph - lat[:, None] / radians_per_pixel - .5, (height, width)).copy()
    img = cv2.remap(pano, mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
    valid = cv2.remap(mask, mx, my, cv2.INTER_NEAREST, borderMode=cv2.BORDER_CONSTANT)
    if fill == 'edge':
        # Deterministic edge extension only; output mask continues to distinguish invented pixels.
        extended = cv2.remap(pano, mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
        img[valid == 0] = extended[valid == 0]
    return img, valid, math.degrees(ph * radians_per_pixel)


FACE_AXES = {
    'right': '+X', 'left': '-X', 'top': '+Y', 'bottom': '-Y', 'front': '+Z', 'back': '-Z'
}


def cubemap(image, mask, size):
    t = 2 * (np.arange(size, dtype=np.float32) + .5) / size - 1
    a, b = np.meshgrid(t, t)
    one = np.ones_like(a)
    vectors = {'front': (a, -b, one), 'back': (-a, -b, -one),
               'right': (one, -b, -a), 'left': (-one, -b, a),
               'top': (a, one, b), 'bottom': (a, -one, -b)}
    h, w = image.shape[:2]
    # Pad longitude for interpolation across the panorama seam without wrapping the poles.
    padded = np.concatenate([image[:, -1:], image, image[:, :1]], axis=1)
    padded_mask = np.concatenate([mask[:, -1:], mask, mask[:, :1]], axis=1)
    result = {}
    for name, (x, y, z) in vectors.items():
        lon = np.arctan2(x, z)
        lat = np.arctan2(y, np.sqrt(x*x + z*z))
        mx = ((lon / (2*np.pi) + .5) * w - .5 + 1).astype(np.float32)
        my = np.clip((.5-lat/np.pi)*h-.5, 0, h-1).astype(np.float32)
        result[name] = (cv2.remap(padded, mx, my, cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE),
                        cv2.remap(padded_mask, mx, my, cv2.INTER_NEAREST, borderMode=cv2.BORDER_REPLICATE))
    return result


def run(args, out):
    print('1/3 Extracting frames...', flush=True)
    frames, info = extract(args.video, args.frames if args.mode == 'rotate' else 1, args.max_frame_width, out/'frames')
    (out/'video_info.json').write_text(json.dumps(info, indent=2), encoding='utf-8')
    warnings = []
    if args.mode == 'rotate':
        print('2/3 Stitching overlapping views (may take several minutes)...', flush=True)
        pano, pmask = stitch(frames)
        save(out/'stitched_strip.png', pano)
        save(out/'stitched_strip_mask.png', pmask)
        equi, valid, vfov = place_panorama(pano, pmask, args.width, args.coverage, args.horizon, args.fill)
        warnings = [
            'Horizontal coverage is USER-SUPPLIED, not estimated or verified from camera pose.',
            'OpenCV may reject disconnected frames; supplied coverage must describe the successfully stitched strip.',
            'Vertical FOV is inferred from strip aspect ratio; horizon position is approximate.',
            'Missing pixels are not recovered; read the coverage masks. This is LDR, not true HDR.',
            'Full-360 seam closure and absolute compass orientation are not guaranteed.'
        ]
    else:
        print('2/3 Using first frame of an already-equirectangular 360 video...', flush=True)
        if not frames or abs(frames[0].shape[1]/frames[0].shape[0]-2) > .03:
            raise ValueError('Equirect mode requires an already stitched 2:1 spherical video.')
        equi = cv2.resize(frames[0], (args.width, args.width//2), interpolation=cv2.INTER_LINEAR)
        valid = np.full(equi.shape[:2], 255, np.uint8)
        vfov = 180
        warnings = ['Source assumed to be a complete equirectangular frame; projection not independently verified.']
    save(out/'panorama.png', equi)
    save(out/'panorama_coverage.png', valid)
    print('3/3 Exporting six cube faces...', flush=True)
    faces = cubemap(equi, valid, args.face_size)
    for name, (img, face_mask) in faces.items():
        save(out/'faces'/f'{name}.png', img)
        save(out/'coverage'/f'{name}.png', face_mask)
    thumb = min(256, args.face_size)
    cross = np.zeros((thumb*3, thumb*4, 3), np.uint8)
    for name, (row, column) in {'top': (0,1), 'left': (1,0), 'front': (1,1), 'right': (1,2), 'back': (1,3), 'bottom': (2,1)}.items():
        tile = cv2.resize(faces[name][0], (thumb,thumb))
        cv2.putText(tile, name, (8,22), cv2.FONT_HERSHEY_SIMPLEX,.55,(255,255,255),1,cv2.LINE_AA)
        cross[row*thumb:(row+1)*thumb,column*thumb:(column+1)*thumb] = tile
    save(out/'cubemap_preview.jpg', cross)
    manifest = {'source': str(args.video.resolve()), 'mode': args.mode, 'panorama_size': [args.width,args.width//2],
                'face_size': args.face_size, 'horizontal_coverage_degrees_assumed': args.coverage if args.mode=='rotate' else 360,
                'vertical_coverage_degrees_inferred': vfov, 'horizon_fraction': args.horizon, 'missing_fill': args.fill,
                'valid_pixel_fraction': float(np.mean(valid > 0)), 'faces': FACE_AXES,
                'orientation': 'Right handed, +Y up, front +Z. Longitude 0 at panorama centre. Images top-left origin.',
                'warnings': warnings}
    (out/'manifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')
    print('\nSUCCESS:', out.resolve(), flush=True)
    print(f'Covered pixels: {manifest["valid_pixel_fraction"]:.1%}. Missing pixels are marked black in coverage masks.')
    for warning in warnings:
        print('NOTE:', warning)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('video', type=Path)
    p.add_argument('--mode', choices=['rotate','equirect'], default='rotate')
    p.add_argument('--coverage', type=float, default=180, help='Approximate horizontal extent of the stitched strip, not automatically known')
    p.add_argument('--horizon', type=float, default=.5, help='Horizon row / strip height')
    p.add_argument('--fill', choices=['black','edge'], default='black', help='edge is synthetic edge replication, not AI reconstruction')
    p.add_argument('--frames', type=int, default=16)
    p.add_argument('--max-frame-width', type=int, default=1280)
    p.add_argument('--width', type=int, default=4096)
    p.add_argument('--face-size', type=int, default=1024)
    p.add_argument('--output-root', type=Path, default=Path(__file__).parent/'output')
    args=p.parse_args()
    cv2.setNumThreads(4)
    if not args.video.is_file():p.error('Video does not exist')
    if not 1 <= args.coverage <= 360:p.error('coverage must be 1..360')
    if not 0 <= args.horizon <= 1:p.error('horizon must be 0..1')
    if not 3 <= args.frames <= 100:p.error('frames must be 3..100')
    if not 128 <= args.width <= 8192 or args.width%2:p.error('width must be even, 128..8192')
    if not 16 <= args.face_size <= 2048:p.error('face-size must be 16..2048')
    if not 256 <= args.max_frame_width <= 4096:p.error('max-frame-width must be 256..4096')
    out=args.output_root/(args.video.stem+'_'+dt.datetime.now().strftime('%Y%m%d_%H%M%S_%f'))
    out.mkdir(parents=True, exist_ok=False)
    try:
        run(args,out)
    except Exception as exc:
        (out/'error.txt').write_text(traceback.format_exc(), encoding='utf-8')
        print(f'ERROR: {exc}\nDiagnostics: {out}', file=sys.stderr)
        return 1
    return 0


if __name__=='__main__':
    raise SystemExit(main())
