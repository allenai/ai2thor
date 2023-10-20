"""
Classes defined for plannign grasping that is specific to Stretch RE1 robot

To install open3d
 !python -m pip install open3d

To install detectron2
 !python -m pip install 'git+https://github.com/facebookresearch/detectron2.git'
"""

import cv2 
import open3d
import json
import numpy as np
from scipy.spatial.transform import Rotation as R

import torch, detectron2
from detectron2 import model_zoo
from detectron2.engine import DefaultPredictor
from detectron2.config import get_cfg

from ai2thor.coco_wordnet import synset_to_ms_coco



class ObjectDetector():
    def __init__(self, device="cpu"):    

        ## DEFAULT INSTANCE SEGMENTATION
        cfg = get_cfg()
        # add project-specific config (e.g., TensorMask) here if you're not running a model in detectron2's core library
        cfg.merge_from_file(model_zoo.get_config_file("COCO-InstanceSegmentation/mask_rcnn_R_50_FPN_3x.yaml"))
        cfg.MODEL.ROI_HEADS.SCORE_THRESH_TEST = 0.5  # set threshold for this model
        # Find a model from detectron2's model zoo. You can use the https://dl.fbaipublicfiles... url as well
        cfg.MODEL.WEIGHTS = model_zoo.get_checkpoint_url("COCO-InstanceSegmentation/mask_rcnn_R_50_FPN_3x.yaml")
        cfg.MODEL.DEVICE = device  #cpu or mps or cuda

        self.predictor = DefaultPredictor(cfg)
        self.predictor_cls_map = synset_to_ms_coco

        ## TODO: read from config
        #if intrinsics is None:
        with open('images/camera_intrinsics/camera_intrinsics_102422073668.txt') as f:
            intr = json.load(f)
        self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
        self.depth_scale = intr["depth_scale"]

        ## TODO: read from config
        r = R.from_quat([0.616, 0.616, -0.346, 0.345]) # camera_color_optical_frame
        t = np.array([-0.017, -0.038, 1.294])
        CameraPose=np.zeros((4,4))
        CameraPose[0:3, 0:3] = r.as_matrix()
        CameraPose[0:3,3] = t
        CameraPose[3,3]=1
        self.CameraPose = CameraPose


    def predict_instance_segmentation(self, rgb):
        rgb = cv2.cvtColor(rgb, cv2.COLOR_BGR2RGB)
        rgb = cv2.rotate(rgb, cv2.ROTATE_90_CLOCKWISE) # it works better for stretch cam
        outputs = self.predictor(rgb)

        predict_classes = outputs["instances"].pred_classes.to("cpu").numpy()
        predict_masks = outputs["instances"].pred_masks.to("cpu").numpy()

        # rotate counter clockwise
        rotated_masks = []
        for i, mask in enumerate(predict_masks):
            mask = mask*1.0
            mask = cv2.rotate(np.array(mask), cv2.ROTATE_90_COUNTERCLOCKWISE)
            rotated_masks.append(mask)
        predict_masks = np.array(rotated_masks)

        return predict_classes, predict_masks
    

    def get_target_mask(self, object_str, rgb):
        # get target object id
        id = self.predictor_cls_map[object_str]

        cls, masks = self.predict_instance_segmentation(rgb)

        # check if target object is detected
        if id in cls:
            return masks[np.where(cls == id)[0]][0] #(1, w, h)-> (w,h)
        else:
            print("Target object " + object_str + " not detected.")
            return None
        
    def get_target_object_pose(self, rgb, depth, mask):
        rgb = np.array(rgb.copy())
        rgbim = open3d.geometry.Image(rgb.astype(np.uint8))

        depth[mask==False] = -0.1
        depth = np.asarray(depth).astype(np.float32) / self.depth_scale
        depthim = open3d.geometry.Image(depth)

        rgbd = open3d.geometry.RGBDImage.create_from_color_and_depth(rgbim, depthim, convert_rgb_to_intensity=False)
        pcd = open3d.geometry.PointCloud.create_from_rgbd_image(rgbd, self.intrinsic)
        center = pcd.get_center()
        bbox = pcd.get_oriented_bounding_box()

        Randt=np.concatenate((bbox.R, np.expand_dims(bbox.center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        lastrow=np.expand_dims(np.array([0,0,0,1]),axis=0)
        objectPoseCamera = np.concatenate((Randt,lastrow)) 

        return self.CameraPose @ objectPoseCamera



class GraspPlanner():
    def __init__(self):
        pass
    
    def plan_lift_extenion(self, object_position, curr_lift_position):
        return object_position[2] + 0.168 - (curr_lift_position-0.21) - 0.41 #meters

    def plan_arm_extension(self, object_position, curr_arm_extension_position):
        return -1*object_position[1] - 0.205 - 0.254 - curr_arm_extension_position + 0.083 # -0.1 + 0.115 

    def plan_base_rotation(self, object_position):
        return -1*np.degrees(np.arctan2(object_position[1], object_position[0])) # bc stretch moves clockwise

    def plan_grasp_trajectory(self, object_position, last_event):
        trajectory = []
        # open grasper 
        trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":100}})
        
        # rotate wrist out
        if np.degrees(last_event.metadata["arm"]["wrist_degrees"]) != 0.0:
            trajectory.appenjd({"action": "MoveWrist", "args": {"move_scalar":  180 + np.degrees(last_event.metadata["arm"]["wrist_degrees"])%180 }})

        # lift
        trajectory.append({"action": "MoveArmBase", "args": {"move_scalar": self.plan_lift_extenion(object_position, last_event.metadata["arm"]["lift_m"])}})
        
        # rotate base
        trajectory.append({"action": "RotateAgent", "args": {"move_scalar": self.plan_base_rotation(object_position) - 90}})
        
        # extend arm
        trajectory.append({"action": "MoveArmExtension", "args": {"move_scalar": self.plan_arm_extension(object_position, last_event.metadata["arm"]["extension_m"])}})
        
        # close grapser
        #trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":-100}})

        return {"action": trajectory}

    