import unittest
import numpy as np
from video_to_skybox import cubemap, place_panorama


class ProjectionTests(unittest.TestCase):
    def test_six_face_centres(self):
        h,w=256,512
        lon=((np.arange(w)+.5)/w-.5)*2*np.pi
        lat=(.5-(np.arange(h)+.5)/h)*np.pi
        x=np.cos(lat[:,None])*np.sin(lon[None,:])
        y=np.broadcast_to(np.sin(lat[:,None]),(h,w))
        z=np.cos(lat[:,None])*np.cos(lon[None,:])
        picture=np.uint8(np.clip((np.stack([x,y,z],axis=-1)+1)*127.5,0,255))
        faces=cubemap(picture,np.full((h,w),255,np.uint8),33)
        expected={'right':[255,127,127],'left':[0,127,127],'top':[127,255,127],
                  'bottom':[127,0,127],'front':[127,127,255],'back':[127,127,0]}
        for n,colour in expected.items():
            self.assertTrue(np.all(np.abs(faces[n][0][16,16].astype(float)-colour)<4),n)
            self.assertTrue(np.all(faces[n][1]==255),n)

    def test_missing_regions_are_masked(self):
        img=np.full((100,400,3),100,np.uint8);mask=np.full((100,400),255,np.uint8)
        out,valid,vfov=place_panorama(img,mask,800,180,.5,'black')
        self.assertEqual(out.shape,(400,800,3))
        self.assertAlmostEqual(vfov,45)
        self.assertEqual(int(valid[200,400]),255)
        self.assertEqual(int(valid[0,400]),0)
        self.assertEqual(int(valid[200,0]),0)
        extended,mask2,_=place_panorama(img,mask,800,180,.5,'edge')
        self.assertTrue(np.all(extended==100))
        np.testing.assert_array_equal(valid,mask2)


if __name__=='__main__':unittest.main()
