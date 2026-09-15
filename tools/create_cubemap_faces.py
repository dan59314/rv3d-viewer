"""Convert a 2:1 equirectangular panorama into OpenGL cubemap faces."""

from pathlib import Path
import argparse

import numpy as np
from PIL import Image


def face_directions(name: str, size: int) -> np.ndarray:
    coordinate = (np.arange(size, dtype=np.float32) + 0.5) * (2.0 / size) - 1.0
    a, b = np.meshgrid(coordinate, coordinate)
    one = np.ones_like(a)
    faces = {
        "px": (one, -b, -a),
        "nx": (-one, -b, a),
        "py": (a, one, b),
        "ny": (a, -one, -b),
        "pz": (a, -b, one),
        "nz": (-a, -b, -one),
    }
    direction = np.stack(faces[name], axis=-1)
    return direction / np.linalg.norm(direction, axis=-1, keepdims=True)


def sample_panorama(panorama: np.ndarray, direction: np.ndarray) -> np.ndarray:
    height, width = panorama.shape[:2]
    x, y, z = direction[..., 0], direction[..., 1], direction[..., 2]
    longitude = np.arctan2(z, x)
    latitude = np.arcsin(np.clip(y, -1.0, 1.0))
    source_x = ((longitude / (2.0 * np.pi) + 0.5) * width) % width
    source_y = np.clip((0.5 - latitude / np.pi) * height, 0, height - 1)

    x0 = np.floor(source_x).astype(np.int32)
    y0 = np.floor(source_y).astype(np.int32)
    x1 = (x0 + 1) % width
    y1 = np.minimum(y0 + 1, height - 1)
    tx = (source_x - x0)[..., None]
    ty = (source_y - y0)[..., None]
    top = panorama[y0, x0] * (1.0 - tx) + panorama[y0, x1] * tx
    bottom = panorama[y1, x0] * (1.0 - tx) + panorama[y1, x1] * tx
    return np.clip(top * (1.0 - ty) + bottom * ty, 0, 255).astype(np.uint8)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--size", type=int, default=1024)
    args = parser.parse_args()

    panorama = np.asarray(Image.open(args.source).convert("RGB"), dtype=np.float32)
    args.output.mkdir(parents=True, exist_ok=True)
    for face in ("px", "nx", "py", "ny", "pz", "nz"):
        pixels = sample_panorama(panorama, face_directions(face, args.size))
        Image.fromarray(pixels, "RGB").save(args.output / f"{face}.jpg", quality=94, subsampling=0)


if __name__ == "__main__":
    main()
