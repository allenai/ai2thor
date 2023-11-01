"""
Classes defined for plannign grasping that is specific to Stretch RE1 robot

To install open3d
 !python -m pip install open3d

To install detectron2
 !python -m pip install 'git+https://github.com/facebookresearch/detectron2.git'

To install OwlVit
 ! pip install transformers

To install Fast SAM
  !pip install git+https://github.com/CASIA-IVA-Lab/FastSAM.git
  !pip install git+https://github.com/openai/CLIP.git
  !pip install ultralytics==8.0.120

  To download model weights
   mkdir model_chekpoints && cd model_checkpoints
   !wget https://huggingface.co/spaces/An-619/FastSAM/resolve/main/weights/FastSAM.pt 
   
"""

import os
import cv2 
import open3d
import json
import numpy as np
from scipy.spatial.transform import Rotation as R

import torch


## CONSTANTS TRANSFORMATION MATRIX
T_ARM_FROM_BASE_50 = np.array([[-9.99936250e-01, -1.12876885e-02,  2.90756694e-04,
        -7.00894177e-02],
       [-6.73963135e-03,  5.75983960e-01, -8.17433211e-01,
        -4.78219868e-02],
       [ 9.05946026e-03, -8.17383059e-01, -5.76023316e-01,
         1.43400645e+00],
       [ 0.00000000e+00,  0.00000000e+00,  0.00000000e+00,
         1.00000000e+00]])

T_ARM_FROM_BASE_30 = np.array([[-0.99602, -0.088905, 0.0066098, -0.067627],
       [ -0.042288, 0.40588, -0.91295, -0.026198],
       [ 0.078483, -0.90959, -0.40802, 1.4357],
       [ 0, 0, 0, 1]])

T_ROTATED_STRETCH_FROM_BASE = np.array([[-0.00069263, 1, -0.0012349, -0.017],
                    [ 0.5214, -0.00069263, -0.85331, -0.038],
                    [ -0.85331, -0.0012349, -0.52139, 1.294],
                    [ 0, 0, 0, 1]])

## CONSTANT CAMERA INTRINSIC 
STRETCH_INTR = {'coeffs': [0.0, 0.0, 0.0, 0.0, 0.0], 'fx': 911.8329467773438, 'fy': 911.9554443359375, 'height': 720, 'ppx': 647.63037109375, 'ppy': 368.0513000488281, 'width': 1280, 'depth_scale': 0.0010000000474974513}
ARM_INTR = {'coeffs': [-0.05686680227518082, 0.06842068582773209, -0.0004524677060544491, 0.0006787769380025566, -0.022475285455584526], 'fx': 640.1092529296875, 'fy': 639.4522094726562, 'height': 720, 'ppx': 652.3712158203125, 'ppy': 368.69549560546875, 'width': 1280, 'depth_scale': 0.0010000000474974513}


class BaseObjectDetector():
    def __init__(self, camera_source="arm30"):
        self.camera_source = camera_source
        self.update_camera_info(camera_source)

    #def predict_instance_segmentation(self, rgb):
    #    raise NotImplementedError

    def update_camera_info(self, camera_source):
        ## TODO: read from config
        if camera_source == "stretch":
            #with open(os.path.join(os.path.dirname(__file__),'camera_intrinsics_102422073668.txt')) as f:
            #    intr = json.load(f)
            intr = STRETCH_INTR
            self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
            self.depth_scale = intr["depth_scale"]
            self.CameraPose = T_ROTATED_STRETCH_FROM_BASE
        elif camera_source == "arm50":
            intr = ARM_INTR
            self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
            self.depth_scale = intr["depth_scale"]
            self.CameraPose = T_ARM_FROM_BASE_50
        elif camera_source == "arm30":
            intr = ARM_INTR
            self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
            self.depth_scale = intr["depth_scale"]
            self.CameraPose = T_ARM_FROM_BASE_30
        else:
            print("Camera source can only be [arm30, arm50 or stretch]")
            print("Please call 'update_camera_info(camera_source)' with the right camera source")

    def get_target_mask(self, object_str, rgb):
        raise NotImplementedError

    def get_target_object_pose(self, rgb, depth, mask):
        if mask is None:
            exit()

        rgb = np.array(rgb.copy())
        rgbim = open3d.geometry.Image(rgb.astype(np.uint8))

        depth[mask==False] = -0.1
        depth = np.asarray(depth).astype(np.float32) / self.depth_scale
        depthim = open3d.geometry.Image(depth)

        rgbd = open3d.geometry.RGBDImage.create_from_color_and_depth(rgbim, depthim, convert_rgb_to_intensity=False)
        pcd = open3d.geometry.PointCloud.create_from_rgbd_image(rgbd, self.intrinsic)
        
        center = pcd.get_center()
        bbox = pcd.get_oriented_bounding_box()
        ##TODO: if len(pcd.points) is zero, there is a problem with a depth image)

        Randt=np.concatenate((bbox.R, np.expand_dims(bbox.center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        lastrow=np.expand_dims(np.array([0,0,0,1]),axis=0)
        objectPoseCamera = np.concatenate((Randt,lastrow)) 

        return self.CameraPose @ objectPoseCamera



class OwlVitSegAnyObjectDetector(BaseObjectDetector):
    """
    https://huggingface.co/docs/transformers/model_doc/owlvit#transformers.OwlViTForObjectDetection
    """

    def __init__(self, fastsam_path, camera_source="arm", device="cpu"):
        super().__init__(camera_source)
        self.device = device

        # initialize OwlVit
        from transformers import OwlViTProcessor, OwlViTForObjectDetection
        self.processor_owlvit = OwlViTProcessor.from_pretrained("google/owlvit-base-patch32")
        self.model_owlvit = OwlViTForObjectDetection.from_pretrained("google/owlvit-base-patch32")
        self.model_owlvit.eval()

        # initialize SegAnything
        from fastsam import FastSAM #, FastSAMPrompt 
        self.model_fastsam = FastSAM(fastsam_path) #os.path.join(os.path.dirname(__file__),'model_checkpoints/FastSAM.pt'))
        self.model_fastsam.to(device=device)

    def get_target_mask(self, object_str, rgb):
        if self.camera_source == "stretch":
            rgb = cv2.rotate(rgb, cv2.ROTATE_90_CLOCKWISE) # it works better for stretch cam

        def predict_object_detection(rgb, object_str):
            with torch.no_grad():
                inputs = self.processor_owlvit(text=object_str, images=rgb, return_tensors="pt")
                outputs = self.model_owlvit(**inputs)

            target_sizes = torch.Tensor([rgb.shape[0:2]])
            results = self.processor_owlvit.post_process_object_detection(outputs=outputs, target_sizes=target_sizes, threshold=0.1)
            boxes = results[0]["boxes"].detach().cpu().numpy() #results[0]["scores"], results[0]["labels"]

            if len(boxes)==0:
                print(f"{object_str} Not Detected.")
                return None

            return [round(i) for i in boxes[0].tolist()] #xyxy

        bbox = predict_object_detection(rgb=rgb, object_str=object_str)

        everything_results = self.model_fastsam(
                rgb,
                device=self.device,
                retina_masks=True,
                imgsz=1024, #1024 is default 
                conf=0.4,
                iou=0.9    
                )

        from fastsam import FastSAMPrompt 
        prompt_process = FastSAMPrompt(rgb, everything_results, device=self.device)
        masks = prompt_process.box_prompt(bboxes=[bbox])
        
        if self.camera_source == "stretch":
            # rotate back
            mask = cv2.rotate(np.array(masks[0]), cv2.ROTATE_90_COUNTERCLOCKWISE)
        else:
            mask = np.array(masks[0])
        return mask


    def get_target_object_pose(self, rgb, depth, mask):
        return super().get_target_object_pose(rgb, depth, mask)


class DoorKnobDetector(OwlVitSegAnyObjectDetector):
    def __init__(self, fastsam_path, camera_source="arm30", device="cpu"):
        super().__init__(fastsam_path, camera_source)

    def get_target_mask(self, rgb, object_str="a photo of a doorknob"):
        return super().get_target_mask(object_str=object_str, rgb=rgb)

    def get_door_pointcloud(self, rgb, depth):
        mask = self.get_target_mask(rgb, object_str="a photo of a door")

        _rgb = np.array(rgb.copy())
        rgbim = open3d.geometry.Image(_rgb.astype(np.uint8))

        _depth = depth.copy()
        _depth[mask==False] = -0.1
        _depth = np.asarray(_depth).astype(np.float32) / self.depth_scale
        depthim = open3d.geometry.Image(_depth)

        rgbd = open3d.geometry.RGBDImage.create_from_color_and_depth(rgbim, depthim, convert_rgb_to_intensity=False)
        pcd = open3d.geometry.PointCloud.create_from_rgbd_image(rgbd, self.intrinsic)
        #pcd_downsampled = pcd.voxel_down_sample(voxel_size=0.05)

        return pcd 

    def get_target_object_pointcloud(self, rgb, depth, mask):
        if mask is None:
            exit()

        _rgb = np.array(rgb.copy())
        rgbim = open3d.geometry.Image(_rgb.astype(np.uint8))

        _depth = depth.copy()
        _depth[mask==False] = -0.1
        _depth = np.asarray(_depth).astype(np.float32) / self.depth_scale
        depthim = open3d.geometry.Image(_depth)

        rgbd = open3d.geometry.RGBDImage.create_from_color_and_depth(rgbim, depthim, convert_rgb_to_intensity=False)
        pcd = open3d.geometry.PointCloud.create_from_rgbd_image(rgbd, self.intrinsic)
        return pcd 

    def get_target_object_pose(self, rgb, depth, mask):
        normval_vector = self.get_center_normal_vector(self.get_door_pointcloud(rgb, depth))

        pcd = self.get_target_object_pointcloud(rgb, depth, mask)
        center = pcd.get_center()
        bbox = pcd.get_oriented_bounding_box()
        ##TODO: if len(pcd.points) is zero, there is a problem with a depth image)

        Randt=np.concatenate((bbox.R, np.expand_dims(bbox.center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        lastrow=np.expand_dims(np.array([0,0,0,1]),axis=0)
        objectPoseCamera = np.concatenate((Randt,lastrow)) 

        preplan_pose = self.plan_pregrasp_pose(objectPoseCamera, normval_vector)
        return [self.CameraPose @ objectPoseCamera, self.CameraPose @ preplan_pose]

    def plan_pregrasp_pose(self, object_pose, normal_vector, distance_m=0.1):
        # Compute the waypoint pose
        waypoint_pose = object_pose.copy()
        translation_offset = distance_m * normal_vector
        waypoint_pose[0:3, 3] += translation_offset
        return waypoint_pose
    
    def get_center_normal_vector(self, pcd):
        # Downsample the point cloud to speed up the normal estimation
        #pcd = pcd.voxel_down_sample(voxel_size=0.05)
        
        # Estimate normals
        pcd.estimate_normals() #search_param=open3d.geometry.KDTreeSearchParamHybrid(radius=0.1, max_nn=30))
        pcd.normalize_normals()

        # Get the center point of the point cloud
        #center_point = np.asarray(pcd.points).mean(axis=0)
        center_point = pcd.get_center()

        # Find the index of the nearest point to the center
        pcd_tree = open3d.geometry.KDTreeFlann(pcd)
        k, idx, _ = pcd_tree.search_knn_vector_3d(center_point, 1)

        # Get the normal at the center point
        return np.asarray(pcd.normals)[idx[0]]


class ObjectDetector(BaseObjectDetector):
    def __init__(self, camera_source="arm30", device="cpu"):    
        super().__init__(camera_source)
        
        # import
        import detectron2
        from detectron2 import model_zoo
        from detectron2.engine import DefaultPredictor
        from detectron2.config import get_cfg

        from ai2thor.coco_wordnet import synset_to_ms_coco

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


    def predict_instance_segmentation(self, rgb):
        #rgb = cv2.cvtColor(rgb, cv2.COLOR_BGR2RGB)
        if self.camera_source == "stretch":
            rgb = cv2.rotate(rgb, cv2.ROTATE_90_CLOCKWISE) # it works better for stretch cam
        outputs = self.predictor(rgb)

        predict_classes = outputs["instances"].pred_classes.to("cpu").numpy()
        predict_masks = outputs["instances"].pred_masks.to("cpu").numpy()

        masks = []
        for mask in predict_masks:
            mask = mask*1.0

            if self.camera_source == "stretch":
                # rotate counter clockwise
                mask = cv2.rotate(np.array(mask), cv2.ROTATE_90_COUNTERCLOCKWISE)

            masks.append(mask)
        predict_masks = np.array(masks)

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
        return super().get_target_object_pose(rgb, depth, mask)




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
        ## TODO: should they be separated? and not command all at once?
        ## TODO: is object position whihtn a reachable range? Add an error margin as an argument to determine.
        
        trajectory = []
        # open grasper 
        trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":100}})
        
        
        # lift1
        trajectory.append({"action": "MoveArmBase", "args": {"move_scalar": self.plan_lift_extenion(object_position, last_event.metadata["arm"]["lift_m"])}})
        
        # rotate base
        trajectory.append({"action": "RotateAgent", "args": {"move_scalar": self.plan_base_rotation(object_position) - 90}})
        
        # extend arm
        trajectory.append({"action": "MoveArmExtension", "args": {"move_scalar": self.plan_arm_extension(object_position, last_event.metadata["arm"]["extension_m"])}})
        
        # TODO: wrist will be out. fix the amount of rotation and sign
        # rotate wrist out
        #if np.degrees(last_event.metadata["arm"]["wrist_degrees"]) != 0.0:
        #    trajectory.append({"action": "MoveWrist", "args": {"move_scalar":  180 + np.degrees(last_event.metadata["arm"]["wrist_degrees"])%180 }})
        trajectory.append({"action": "WristTo", "args": {"move_to":  0}})

        # close grapser
        #trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":-100}})

        return {"action": trajectory}

    

class DoorKnobGraspPlanner(GraspPlanner):
    def plan_grasp_trajectory(self, object_waypoint_position, last_event):
        first_actions = super().plan_grasp_trajectory(object_waypoint_position, last_event)
        second_actions = {"action": [
          {"action": "MoveArmExtension", "args": {"move_scalar": 0.1}}  
        ]}

        return first_actions, second_actions

        
