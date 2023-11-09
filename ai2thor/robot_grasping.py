"""
Classes defined for planning grasping that is specific to Stretch RE1 robot

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
import math
import torch
import matplotlib.pyplot as plt

## CONSTANTS TRANSFORMATION MATRIX
T_ARM_FROM_BASE_188 = np.array([[   -0.99906,    -0.04335,  -0.0003883,   -0.047584],
                                [  -0.020913,     0.48978,     -0.8716,   -0.053311],
                                [   0.037974,    -0.87077,    -0.49022,      1.4462],
                                [          0,           0,           0,           1]])


T_ARM_FROM_BASE_205 = np.array([[   -0.99652,   -0.080247,   -0.022519,   -0.055535],
       [  -0.023487,     0.52961,    -0.84792,   -0.053421],
       [   0.079969,    -0.84444,    -0.52965,      1.4676],
       [          0,           0,           0,           1]])

T_ROTATED_STRETCH_FROM_BASE = np.array([[-0.00069263, 1, -0.0012349, -0.017],
                    [ 0.5214, -0.00069263, -0.85331, -0.038],
                    [ -0.85331, -0.0012349, -0.52139, 1.294],
                    [ 0, 0, 0, 1]])

## CONSTANT CAMERA INTRINSIC 
STRETCH_INTR = {'coeffs': [0.0, 0.0, 0.0, 0.0, 0.0], 'fx': 911.8329467773438, 'fy': 911.9554443359375, 'height': 720, 'ppx': 647.63037109375, 'ppy': 368.0513000488281, 'width': 1280, 'depth_scale': 0.0010000000474974513}
ARM_INTR = {'coeffs': [-0.05686680227518082, 0.06842068582773209, -0.0004524677060544491, 0.0006787769380025566, -0.022475285455584526], 'fx': 640.1092529296875, 'fy': 639.4522094726562, 'height': 720, 'ppx': 652.3712158203125, 'ppy': 368.69549560546875, 'width': 1280, 'depth_scale': 0.0010000000474974513}


class BaseObjectDetector():
    def __init__(self, camera_source="arm205"):
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
        elif camera_source == "arm205":
            intr = ARM_INTR
            self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
            self.depth_scale = intr["depth_scale"]
            self.CameraPose = T_ARM_FROM_BASE_205
        elif camera_source == "arm188":
            intr = ARM_INTR
            self.intrinsic = open3d.camera.PinholeCameraIntrinsic(intr["width"],intr["height"],intr["fx"],intr["fy"],intr["ppx"],intr["ppy"])    
            self.depth_scale = intr["depth_scale"]
            self.CameraPose = T_ARM_FROM_BASE_188        
        else:
            print("Camera source can only be [arm205, arm188 or stretch]")
            print("Please call 'update_camera_info(camera_source)' with the right camera source")

    def get_target_mask(self, object_str, rgb):
        raise NotImplementedError

    def get_target_object_pose(self, rgb, depth, mask, visualize=False):
        if mask is None:
            exit()

        rgb = np.array(rgb.copy())
        rgbim = open3d.geometry.Image(rgb.astype(np.uint8))

        _depth = depth.copy()
        _depth[mask==False] = -0.1
        _depth = np.asarray(_depth).astype(np.float32) / self.depth_scale
        depthim = open3d.geometry.Image(_depth)

        rgbd = open3d.geometry.RGBDImage.create_from_color_and_depth(rgbim, depthim, convert_rgb_to_intensity=False)
        pcd = open3d.geometry.PointCloud.create_from_rgbd_image(rgbd, self.intrinsic)
        print("pcd pointcloud numbers: ", len(pcd.points))
        pcd.remove_statistical_outlier(nb_neighbors=20, std_ratio=2.0)
        print("pcd pointcloud numbers after outlier removal: ", len(pcd.points))

        center = pcd.get_center()
        bbox = pcd.get_oriented_bounding_box()
        
        ##TODO: if len(pcd.points) is zero, there is a problem with a depth image)
        Randt=np.concatenate((bbox.R, np.expand_dims(center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        lastrow=np.expand_dims(np.array([0,0,0,1]),axis=0)
        objectPoseCamera = np.concatenate((Randt,lastrow)) 

        Randt=np.concatenate((bbox.R, np.expand_dims(bbox.center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        objectPoseCamera1 = np.concatenate((Randt,lastrow)) 
        print("Center vs bbox center:", (self.CameraPose @ objectPoseCamera)[:3,3], (self.CameraPose @ objectPoseCamera1)[:3,3])

        ObjectPose = self.CameraPose @ objectPoseCamera
        ObjectPose1 = self.CameraPose @ objectPoseCamera1
        ObjectPose1[0,3] = ObjectPose[0,3]
        ObjectPose1[1,3] = ObjectPose[1,3]


        if visualize:
            color = np.array([0,255,0], dtype='uint8')
            masked_img = np.where(mask[...,None], color, rgb)
            out = cv2.addWeighted(rgb, 0.65, masked_img, 0.35,0)
            plt.imshow(out)
            plt.show()

            obj_center = open3d.geometry.TriangleMesh.create_sphere(radius=0.015) #create a small sphere to represent point
            obj_center.translate(ObjectPose1[:3,3])
            obj_center.paint_uniform_color([255,0,0])

            coord = open3d.geometry.TriangleMesh().create_coordinate_frame()

            # TODO: maybe add robot mesh at origin
            pcd_base = pcd.transform(self.CameraPose)
            
            vis = open3d.visualization.Visualizer()
            vis.create_window()
            vis.add_geometry(pcd_base)
            vis.add_geometry(obj_center)
            vis.add_geometry(coord)

            vis.run()
            vis.destroy_window() # this kills Jupyter Notebook kernel for some reason

        return ObjectPose1 #self.CameraPose @ objectPoseCamera



class OwlVitSegAnyObjectDetector(BaseObjectDetector):
    """
    https://huggingface.co/docs/transformers/model_doc/owlvit#transformers.OwlViTForObjectDetection
    """

    def __init__(self, fastsam_path, camera_source="arm205", device="cpu"):
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


    def predict_object_detection(self, rgb, object_str):
        with torch.no_grad():
            inputs = self.processor_owlvit(text=object_str, images=rgb, return_tensors="pt")
            outputs = self.model_owlvit(**inputs)

        target_sizes = torch.Tensor([rgb.shape[0:2]])
        results = self.processor_owlvit.post_process_object_detection(outputs=outputs, target_sizes=target_sizes, threshold=0.1)
        boxes = results[0]["boxes"].detach().cpu().numpy() #results[0]["scores"], results[0]["labels"]
        scores = results[0]["scores"].detach().cpu().numpy()

        if len(boxes)==0:
            print(f"{object_str} Not Detected.")
            return None
        
        ind = np.argmax(scores)
        return [round(i) for i in boxes[ind].tolist()] #xyxy
    

    def get_target_mask(self, object_str, rgb):
        if self.camera_source == "stretch":
            rgb = cv2.rotate(rgb, cv2.ROTATE_90_CLOCKWISE) # it works better for stretch cam

        bbox = self.predict_object_detection(rgb=rgb, object_str=object_str)
    
        if bbox is None or len(bbox) == 0:
            return None

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


    def get_target_object_pose(self, rgb, depth, mask, visualize=False):
        return super().get_target_object_pose(rgb, depth, mask, visualize=visualize)


class DoorKnobDetector(OwlVitSegAnyObjectDetector):
    def __init__(self, fastsam_path, camera_source="arm205", device="cpu"):
        super().__init__(fastsam_path, camera_source)
        self.door_mask = None 

    def predict_object_detection(self, rgb, object_str, point):
        with torch.no_grad():
            inputs = self.processor_owlvit(text=object_str, images=rgb, return_tensors="pt")
            outputs = self.model_owlvit(**inputs)

        target_sizes = torch.Tensor([rgb.shape[0:2]])
        results = self.processor_owlvit.post_process_object_detection(outputs=outputs, target_sizes=target_sizes, threshold=0.1)
        boxes = results[0]["boxes"].detach().cpu().numpy() #results[0]["scores"], results[0]["labels"]
        scores = results[0]["scores"].detach().cpu().numpy()
        
        if len(boxes)==0:
            print(f"{object_str} Not Detected.")
            return None
        
        combined_data = list(zip(boxes, scores))
        sorted_data = sorted(combined_data, key=lambda x: x[1], reverse=True)
        sorted_boxes, _ = zip(*sorted_data)

        for box in sorted_boxes:
            # Check if the point is within the bounding box
            if box[0] <= point[0] <= box[2] and box[1] <= point[1] <= box[3]:
                return [round(i) for i in box.tolist()]
        return None 

    def get_target_mask(self, rgb, object_str="a photo of a doorknob"):
        doorknob_bbox = super().predict_object_detection(rgb, object_str) #xyxy
        x_center = round(doorknob_bbox[0] + (doorknob_bbox[2]-doorknob_bbox[0])/2)
        y_center = round(doorknob_bbox[1] + (doorknob_bbox[3]-doorknob_bbox[1])/2)
        
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
        doorknob_masks = prompt_process.box_prompt(bboxes=[doorknob_bbox])

        door_bbox = self.predict_object_detection(rgb, object_str="a photo of a door", point=[x_center, y_center]) #xyxy
        door_masks = prompt_process.box_prompt(bboxes=[door_bbox])
        door_mask = door_masks[0]

        #door_masks = prompt_process.point_prompt(points=[[x_center, y_center]],  pointlabel=[0]) # 0:background, 1:foreground
        #door_masks = prompt_process.text_prompt(text="a photo of a door")
        
        if self.camera_source == "stretch":
            # rotate back
            doorknob_mask = cv2.rotate(np.array(doorknob_masks[0]), cv2.ROTATE_90_COUNTERCLOCKWISE)
            door_mask = cv2.rotate(door_mask, cv2.ROTATE_90_COUNTERCLOCKWISE)
        else:
            doorknob_mask = np.array(doorknob_masks[0])
       
        self.door_mask = door_mask
        return doorknob_mask

    def get_door_pointcloud(self, rgb, depth):
        if self.door_mask is None :
            self.get_target_mask(rgb)
        mask = self.door_mask #elf.get_target_mask(rgb, object_str="a photo of a door")

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

    def get_target_object_pose(self, rgb, depth, mask, distance_m=0.215, visualize=False): # -0.205 
        pcd = self.get_target_object_pointcloud(rgb, depth, mask)
        ##TODO: if len(pcd.points) is zero, there is a problem with a depth image)

        self.center = pcd.get_center()
        bbox = pcd.get_oriented_bounding_box()

        Randt=np.concatenate((bbox.R, np.expand_dims(bbox.center, axis=1)),axis=1) # pitfall: arrays need to be passed as a tuple
        lastrow=np.expand_dims(np.array([0,0,0,1]),axis=0)
        objectPoseCamera = np.concatenate((Randt,lastrow)) 
        objectPoseBase = self.CameraPose @ objectPoseCamera

        # Get Normal oriented towards camera
        normval_vector = self.get_center_normal_vector(self.get_door_pointcloud(rgb, depth))
        preplan_pose = self.plan_pregrasp_pose(objectPoseCamera, normval_vector, distance_m)
        preplan_pose_base = self.CameraPose @ preplan_pose
        
        # TODO: maybe compare Z axis and recompute if very off

        if visualize:
            obj_center = open3d.geometry.TriangleMesh.create_sphere(radius=0.005) #create a small sphere to represent point
            obj_center.translate(objectPoseBase[:3,3])
            obj_center.paint_uniform_color([255,0,0])

            preplan_center = open3d.geometry.TriangleMesh.create_sphere(radius=0.005) #create a small sphere to represent point
            preplan_center.translate(preplan_pose_base[:3,3])  
            preplan_center.paint_uniform_color([255,0,0])

            coord = open3d.geometry.TriangleMesh().create_coordinate_frame()
            
            pcd_base = pcd.transform(self.CameraPose)
            
            vis = open3d.visualization.Visualizer()
            vis.create_window()
            vis.add_geometry(pcd_base)
            vis.add_geometry(obj_center)
            vis.add_geometry(preplan_center)
            vis.add_geometry(coord)
            vis.run()
            vis.destroy_window()

        return [objectPoseBase, preplan_pose_base]

    def plan_pregrasp_pose(self, object_pose, normal_vector, distance_m=0.215):
        # GraspCenter to Arm Offset = 0.205
        # Compute the waypoint pose
        waypoint_pose = object_pose.copy()
        translation_offset = distance_m * normal_vector
        waypoint_pose[0:3, 3] += translation_offset
        return waypoint_pose
    
    def get_center_normal_vector(self, pcd):
        # Downsample the point cloud to speed up the normal estimation
        #pcd = pcd.voxel_down_sample(voxel_size=0.05)

        plane_model, inliers = pcd.segment_plane(distance_threshold=0.01,
                                                ransac_n=3,
                                                num_iterations=1000)
        [a, b, c, d] = plane_model

        if c > 0: # always orient toward camera
            return -1*np.asanyarray([a, b, c])
        return np.asanyarray([a, b, c])
    
        """
        # Estimate normals
        pcd.estimate_normals() #search_param=open3d.geometry.KDTreeSearchParamHybrid(radius=0.1, max_nn=30))
        pcd.normalize_normals()
        pcd.orient_normals_towards_camera_location()

        # Find the index of the nearest point to the center
        pcd_tree = open3d.geometry.KDTreeFlann(pcd)
        k, idx, _ = pcd_tree.search_knn_vector_3d(self.center, 1)

        # Get the normal at the center point
        print("Normal near doorknob cneter: ", np.asarray(pcd.normals)[idx[0]])
            
        # Get the center point of the point cloud
        #center_point = np.asarray(pcd.points).mean(axis=0)
        center_point = pcd.get_center()

        # Find the index of the nearest point to the center
        pcd_tree = open3d.geometry.KDTreeFlann(pcd)
        k, idx, _ = pcd_tree.search_knn_vector_3d(center_point, 1)

        # Get the normal at the center point
        return np.asarray(pcd.normals)[idx[0]]
        """

class ObjectDetector(BaseObjectDetector):
    def __init__(self, camera_source="arm205", device="cpu"):    
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
        

    def get_target_object_pose(self, rgb, depth, mask, visualize=False):
        return super().get_target_object_pose(rgb, depth, mask, visualize=visualize)




class GraspPlanner():
    """ Naive grasp Planner """
    def __init__(self):
        pass
    
    def plan_lift_extenion(self, object_position, curr_lift_position):
        return object_position[2] + 0.168 - (curr_lift_position-0.21) - 0.41 #meters

    def plan_arm_extension(self, object_position, curr_arm_extension_position):
        # assumes wrist rotation is at 0 degree position 
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
    def __init__(self):
        super().__init__()

        ## 188 constants
        self.gripper_length = 0.205
        self.gripper_heihgt = 0.138

        self.wrist_yaw_from_base = 0.003#25 #-0.020 # -0.025 # FIXED - should be.
        self.arm_offset = 0.155 #0.140
        self.lift_base_offset = 0.192 #base to lift
        self.lift_wrist_offset = 0.028


    def plan_base_rotation(self, object_position):
        # positive rotation clockwise
        ### TODO: MAKE SURE THIS WORKS FOR BOTH POSITIVE X, NEGATIVE X 
        return np.degrees(np.arctan2(-object_position[1], object_position[0])) - 90 # bc stretch moves clockwise

    def get_gripper_center_position(self, last_event):
        wrist_yaw = last_event.metadata["arm"]["wrist_degrees"] # but is actually in radians

        position = np.zeros(3)

        # x axis FIXED
        wrist_to_gripper_offset_x = -np.sin(np.deg2rad(wrist_yaw)) * self.gripper_length # 0.205 #TODO to be correct. it should be cos(angle)*offset
        position[0] = self.wrist_yaw_from_base + wrist_to_gripper_offset_x

        #TODO: check if this is correct
        # y depends on Arm Extension
        wrist_to_gripper_offset_y = -np.cos(np.deg2rad(wrist_yaw)) * self.gripper_length # 0.205 #TODO to be correct. it should be cos(angle)*offset
        position[1] = -(last_event.metadata["arm"]["extension_m"] + self.arm_offset) + wrist_to_gripper_offset_y

        # z depends on Lift
        position[2] = last_event.metadata["arm"]["lift_m"] + self.lift_base_offset + self.lift_wrist_offset - self.gripper_heihgt

        #rotation = np.zeros((3,3))
        print(f"Gripper center position from base frame: {position}")
        return position


    def isReachable(self, target_position, last_event, threshold=0.25):
        gripper_position = self.get_gripper_center_position(last_event)

        def distance_between_points(p1, p2):
            x1, y1, z1 = p1
            x2, y2, z2 = p2
            return math.sqrt((x2 - x1)**2 + (y2 - y1)**2 + (z2 - z1)**2)
        
        ## Not reachable when
        #1. grasper center <-> object is beyond a threashold
        if distance_between_points(target_position, gripper_position) >= threshold:
            return False
        return True 
        
    def plan_grasp_trajectory(self, object_waypoints, last_event):
        object_pose, pregrasp_pose = object_waypoints
        
        object_position = object_pose[0:3,3]
        pregrasp_position = pregrasp_pose[0:3,3]
        #pregrasp_position[2] = object_position[2]

        ## FIRST ACTION
        # 1. move a wrist to a pregrasp position
        # 2. rotate wrist to align with the object center
        trajectory = []

        # open grasper 
        trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":100}})
        
        # rotate base
        # TODO: omit rotate. and if not reachable call it failure
        #self.plan_base_rotation(pregrasp_position)# - 90
        #self.plan_base_rotation(object_position)# - 90

        trajectory.append({"action": "RotateAgent", "args": {"move_scalar": self.plan_base_rotation(pregrasp_position)}})
        
        ## TODO: LIFT OFFSET 
        # lift
        lift_offset = 0.1
        #trajectory.append({"action": "MoveArmBase", "args": {"move_scalar": lift_offset + self.plan_lift_extenion(pregrasp_position, last_event.metadata["arm"]["lift_m"])}}) 
        trajectory.append({"action": "MoveArmBase", "args": {"move_scalar": lift_offset + self.plan_lift_extenion(object_position, last_event.metadata["arm"]["lift_m"])}}) 
        
        
        # rotate wrist - stretch wrist moves clockwise
        # pregrasp position 's -Y direction is X 
        # pregrasp position 's -X direction is Y 
        wrist_to_joint_offset=0#0.070 #0.05
        x_delta, y_delta = (object_position - pregrasp_position)[0:2]
        wrist_offset = np.degrees(np.arctan2(-x_delta-wrist_to_joint_offset, -y_delta)) # arctan2(y,x)
        trajectory.append({"action": "WristTo", "args": {"move_to":  wrist_offset}})

        # extend arm
        arm_offset = 0.220 #0.205 #0.205
        #arm_offset *= np.cos(wrist_offset)
        #print("wrist length cos: ", np.cos(np.deg2rad(wrist_offset)) * self.gripper_length)
        #arm_offset = self.gripper_length #- np.cos(np.deg2rad(wrist_offset)) * self.gripper_length
        delta_ext = arm_offset + self.plan_arm_extension(pregrasp_position, last_event.metadata["arm"]["extension_m"])
        
        #2. needs to move the mobiel base 
        if (delta_ext + last_event.metadata["arm"]["extension_m"]) >= 0.52 or (last_event.metadata["arm"]["extension_m"] + delta_ext) < 0.0:
            print("Need to extend to far. Not Reachable.", delta_ext)
            return False, []

        trajectory.append({"action": "MoveArmExtension", "args": {"move_scalar": delta_ext}})
        first_actions = {"action": trajectory}


        ## SECOND ACTION
        # 1. move arm base down a predetermined to object center
        # 2. grasp
        second_actions = {"action": [
          {"action": "MoveArmBase", "args": {"move_scalar": -lift_offset-0.02}}  
        ]}
        
        # close grapser
        #trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":-100}})

        return True, [first_actions, second_actions]



class VIDAGraspPlanner(GraspPlanner):
    def __init__(self):
        super().__init__()

        ## 188 constants
        self.wrist_yaw_from_base = 0.003 # 25 #-0.020 # -0.025 # FIXED - should be.
        self.arm_offset = 0.155 # 0.140
        self.lift_base_offset = 0.192 # base to lift
        self.lift_wrist_offset = 0.028

    def plan_lift_extenion(self, object_position, curr_lift_position):
        lift_object_offset = -0.025 # to grasp a little lower than the estimated cetner
        return (object_position[2]+lift_object_offset) + 0.168 - (curr_lift_position-0.21) - 0.41 #meters

    def get_wrist_position(self, last_event):
        position = np.zeros(3)
        
        # x axis FIXED
        position[0] = self.wrist_yaw_from_base

        #TODO: check if this is correct
        # y depends on Arm Extension
        position[1] = -(last_event.metadata["arm"]["extension_m"] + self.arm_offset)

        # z depends on Lift
        position[2] = last_event.metadata["arm"]["lift_m"] + self.lift_base_offset + self.lift_wrist_offset

        #rotation = np.zeros((3,3))
        print(f"Wrist position from base frame: {position}")
        return position
    

    def find_points_on_y_axis(self, p2, distance=0.210): #205 210 worked for most too. 0.220 works for apple 0.208
        def distance_between_points(p1, p2):
            x1, y1 = p1
            x2, y2 = p2
            return math.sqrt((x2 - x1)**2 + (y2 - y1)**2)
        
        sqrt_diff = distance**2 - p2[0]**2
        if sqrt_diff < 0:
            return []
        
        y1_1 = p2[1] + math.sqrt(distance**2 - p2[0]**2)
        y1_2 = p2[1] - math.sqrt(distance**2 - p2[0]**2)

        # print("new points 1: ", y1_1, distance_between_points(p2,[0.0, y1_1]))
        # print("new ppints 2: ", y1_2, distance_between_points(p2,[0.0, y1_2]))

        new_points = []
        if abs(distance_between_points(p2,[0.0, y1_1]) - distance) <= 0.0005:
            new_points.append([0.0, y1_1])
        if abs(distance_between_points(p2,[0.0, y1_2]) - distance) <= 0.0005:
            new_points.append([0.0, y1_2])

        return new_points #returns bigger value first - closer to 0 means it's cloer to base


    def plan_grasp_trajectory(self, object_position, last_event, distance=0.210):
        wrist_position = self.get_wrist_position(last_event)

        x_delta, y_delta, z_delta = (object_position - wrist_position)
        new_wrist_positions = self.find_points_on_y_axis([x_delta, y_delta], distance)

        isReachable=False
        trajectory = []

        curr_arm = last_event.metadata["arm"]["extension_m"]
        
        for new_position in new_wrist_positions:
            new_arm_position = -new_position[1] 
            if not isReachable and (curr_arm + new_arm_position) < 0.5193114280700684 and (curr_arm + new_arm_position) > 0.0:
                # open grasper 
                trajectory.append({"action": "MoveGrasp", "args": {"move_scalar":100}})

                # TODO: check z_delta before  
                # - will it hit the object? It does sometimes...so might have to lift a little
                # rotate wrist - stretch wrist moves clockwise
                # pregrasp position 's -Y direction is X
                # pregrasp position 's -X direction is Y
                #last_event.metadata["arm"]["extension_m"] += new_arm_position
                #wrist_position = self.get_wrist_position(last_event)
                #x_delta, y_delta, z_delta = (object_position - wrist_position)
                y_delta = -1*abs(y_delta + new_arm_position) 
                wrist_offset = np.degrees(np.arctan2(-x_delta, -y_delta)) # arctan2(y,x)
                if wrist_offset >= 75.0: #max Wrist Rotation
                    trajectory = []
                    isReachable=False
                    continue 
                trajectory.append({"action": "WristTo", "args": {"move_to":  wrist_offset}})

                # TODO: check z_delta before  - will it hit the object?
                # extend arm
                #wrist_to_gripper_offset = 0.205 - np.cos(np.deg2rad(wrist_offset)) * 0.205 # 0.205 #TODO to be correct. it should be cos(angle)*offset
                #print("wrist gripper offset:", wrist_to_gripper_offset)
                trajectory.append({"action": "MoveArmExtension", "args": {"move_scalar": new_arm_position}})                    
                
                # lift - will it hit the object? most likely the arm is higher than the object....
                trajectory.append({"action": "MoveArmBase", "args": {"move_scalar": self.plan_lift_extenion(object_position, last_event.metadata["arm"]["lift_m"])}})
                isReachable=True 
    
            
        return isReachable, {"action": trajectory}


        
