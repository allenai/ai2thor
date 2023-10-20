"""
Re-implementing Controller class from https://github.com/allenai/ai2thor/blob/main/ai2thor/robot_controller.py

The current implementation assumes there is only one Agent and its corresponding Event to be created (not using MulitAgentEvent)

by Yejin Kim
"""

import cv2
import numpy as np
import time
import threading 
import math

from ai2thor.server import Event, MultiAgentEvent, DepthFormat
#from ai2thor.interact import InteractiveControllerPrompt, DefaultActions

import grpc
from ai2thor.api import image_pb2_grpc, image_pb2, robot_state_pb2_grpc, robot_state_pb2, robot_command_pb2, robot_command_pb2_grpc

enum_to_string = {
    0 : "bgr",
    1 : "depth"
}

MAX_MESSAGE_LENGTH = 5*1280*720

def timestamp1_is_bigger_timestamp2(ts1, ts2):
    #print("ts2 - ts1", ts2.seconds - ts1.seconds, ts2.nanos-ts1.nanos)
    if (ts2.seconds - ts1.seconds) > 0: #float(ts1.seconds) > float(ts2.seconds):
        return False
    if (ts2.nanos - ts1.nanos) > 0: #float(ts1.nanos) > float(ts2.nanos):
        return False
    return True 

def difference_in_timestamps(ts1, ts2):
    dsec = ts1.seconds - ts2.seconds
    dnano = ts1.nanos - ts2.nanos
    return dsec + dnano * 10**-9

def decode_image(buffer, pixel_format, width=None, height=None): #cv2.IMREAD_COLOR):
    #print(buffer)
    if pixel_format == 0:
        nparr = np.frombuffer(buffer, dtype= np.uint8)#.reshape(720,1280,3)
        image = cv2.imdecode(nparr, cv2.IMREAD_UNCHANGED)    
    else:
        nparr = np.frombuffer(buffer, dtype=np.uint16)#, np.uint8)#.reshape(720,1280,3)
        #image = cv2.imdecode(nparr, cv2.IMREAD_UNCHANGED)
        #print(nparr.min(), nparr.max())
        image = nparr.reshape(height, width,1)
    return image 



PRINT_TIMESTAMPS = False
#TODO: type of observation and states should be passed through a config/yaml file
class Controller(object):
    def __init__(self, host="", port=50051, width=1280, height=720, agent_id=0, multi_thread=False,
                get_depth=True, depth_format=DepthFormat.Meters,
                camera_sources = ["arm","nav"], #"stretch
                camera_near_plane=0.1, camera_far_plane=20):
        
        self.camera_sources = camera_sources
        self.robot_client = RobotClient(robot_ip=host, port=str(port), width=width, height=height, multi_thread=multi_thread, get_depth=get_depth, camera_sources=camera_sources)
        self.last_event = {}
        self.last_action = {}

        self.sequence_id = 0 #where does this get updated? what is it for?
        self.agent_id = agent_id 
        
        self.screen_width = width 
        self.screen_height = height

        self.get_depth = get_depth
        self.depth_format = depth_format
        self.camera_near_plane = camera_near_plane
        self.camera_far_plane = camera_far_plane

        # server.py Event class
        self._metadata = {
                    "agent": {},
                    "arm": {},
                    "agentId": 0,
                    "objects": [],
                    "screenHeight": height,
                    "screenWidth": width,
                    "depthWidth": width,
                    "depthHeight": height,
                    # 'depthWidth': self.kinect_camera.depth_width,
                    # 'depthHeight': self.kinect_camera.depth_height,
                    "lastAction": self.last_action,
                    "lastActionSuccess": None,
                    "errorMessage": None,
                    "errorCode": None,
                    # 'position': dict(x=0, y=0, z=0), TODO is this the one we need to change?
                    # 'rotation': {'x':0, 'y':0, 'z':z_rotation},
                    "thirdPartyCameras": [
                        {
                            "screenWidth": width,
                            "screenHeight": height,
                            "depthWidth": width,
                            "depthHeight": height,
                        }
                    ],
                    "actionReturn": []
            }
            

    def update_screen_dimension(self, width, height):
        self.screen_width = width
        self.screen_height = height
        self.robot_client.set_obs_shape(width, height)
        if self.robot_client.multi_thread:
            self.robot_client.flag_action_timestamp(None)

    def update_camera_sources(self, camera_sources=["nav","arm","stretch"]):
        assert(type(camera_sources)==list)
        self.camera_sources = camera_sources
        self.robot_client.camera_sources = camera_sources
        if self.robot_client.multi_thread:
            self.robot_client.flag_action_timestamp(None)

    def stop(self):
        pass 

    def start(self):
        pass 

    def reset(self):
        self.sequence_id = 0
        return self.last_event

    def step(self, action=None, **actino_args):
        start_time = time.time()
        ## TODO: use config information to collect and return obs and states
        ## Assuming there is only one Agent and its corresponding Event to be created (not using MulitAgentEvent)
        
        # send action
        ## TODO: single dict with "action" and "args" keyword
        if action is not None and type(action) is not dict:
            action = dict(action=[action])

        if action is not None:
            action.update(actino_args)

        (robot_state, images), (success, error_message, timestamp) = self.robot_client.step(action)

        # gather and structure data
        #self._metadata["sequenceId"] = self.sequence_id
        self._metadata["agent"] = {
                        "cameraHorizon": 0.0,
                        "position": robot_state["agent"]["position"],
                        "rotation": robot_state["agent"]["rotation"]}
        self._metadata["arm"] = robot_state["arm"]
        self._metadata["lastAction"]= action
        self._metadata["lastActionSuccess"] = success
        self._metadata["errorMessage"] = error_message
            
        
        event = Event(self._metadata)
        if "nav" in self.camera_sources:
            event.frame = cv2.cvtColor(images["nav"]["bgr"]["data"], cv2.COLOR_BGR2RGB)
        if "arm" in self.camera_sources:
            event.third_party_camera_frames.append(images["arm"]["bgr"]["data"])
        if "stretch" in self.camera_sources:
            event.third_party_camera_frames.append(images["stretch"]["bgr"]["data"])

        #event.frame = add_image(images["nav"]["bgr"]["data"], flip_y=False, flip_rb_colors=False)
        #event.add_third_party_camera_image_robot(images["arm"]["bgr"]["data"], self.screen_width, self.screen_height)
    
        if self.get_depth:
            ## TODO: Depth Format is passed to multiply....
            ## Realsense depth scale = 0.001 (m)
            #event.add_third_party_image_depth_robot(images["arm"]["depth"]["data"], dtype=np.float64, flip_y=False, depth_format=self.depth_format, depth_width=self.screen_width, depth_height=self.screen_height)
            #event.add_image_depth_robot(images["nav"]["depth"]["data"], self.depth_format, camera_near_plane=self.camera_near_plane, camera_far_plane=self.camera_far_plane, depth_width=self.screen_width, depth_height=self.screen_height, flip_y=False, dtype=np.float64)
            if "nav" in self.camera_sources:
                event.depth_frame = 0.001 * images["nav"]["depth"]["data"]
            if "arm" in self.camera_sources:
                event.third_party_depth_frames.append(0.001 * images["arm"]["depth"]["data"])
            if "stretch" in self.camera_sources:
                event.third_party_depth_frames.append(0.001 * images["stretch"]["depth"]["data"])


        self.last_event = event 
        print("Last Event constructed. ", time.time()-start_time)
        return self.last_event


    def interact(self):
        pass 

    
    @staticmethod
    def _display_step_event(event):
        pass


class RobotClient():
    def __init__(self, multi_thread=False, robot_ip="172.16.121.205", port="50051", width=1280, height=720, get_depth=False, camera_sources=["nav", "arm"]):
        channel = grpc.insecure_channel(robot_ip+':'+port,
                                          options=[
        ('grpc.max_send_message_length', MAX_MESSAGE_LENGTH),
        ('grpc.max_receive_message_length', MAX_MESSAGE_LENGTH)])

        # all services
        self.obs_stub = image_pb2_grpc.ImageServiceStub(channel)
        self.state_stub = robot_state_pb2_grpc.RobotStateServiceStub(channel)
        self.command_stub = robot_command_pb2_grpc.RobotCommandServiceStub(channel)

        # init variables
        self.images = {}
        for cam_source in camera_sources:
            self.images[cam_source] = {"bgr": {"timestamp":None, "data":None}, "depth": {"timestamp":None, "data":None}}
        #self.images["arm"] = {"bgr": {"timestamp":None, "data":None}, "depth": {"timestamp":None, "data":None}}
        #self.images["nav"] = {"bgr": {"timestamp":None, "data":None}, "depth": {"timestamp":None, "data":None}}
        self.set_obs_shape(width, height)
        self.get_depth = get_depth # TODO:
        self.camera_sources = camera_sources

        self.robot_state = {}
        #self.robot_state["timestamp"] = None
        #self.robot_state["agent"] = {}
        #self.robot_state["arm"] = {}

        self.robot_last_action = {}
        self.robot_last_action["timestamp"] = None 
        self.robot_last_action["actions"] = {}

        self._action_timestamp = None
        self._timebound = 1.0 # check if recieved responses are within specific time interval

        # all threads
        self.multi_thread = multi_thread
        self._recieved_data = {}
        if self.multi_thread:
            robot_state_thread= threading.Thread(target=self.run_state)
            robot_state_thread.start()
            self._recieved_data["robot_state"] = False

            self._recieved_data["images"] = {}
            camera_threads = []
            if not self.get_depth:
                for cam_source in camera_sources:
                    cam_thread = threading.Thread(target=self.run_observations, args=(cam_source, 0))
                    cam_thread.start()
                    camera_threads.append(cam_thread)
                    self._recieved_data["images"][cam_source + "bgr"] = False
                #camera_thread1 = threading.Thread(target=self.run_observations, args=("nav", 0))
                #camera_thread1.start()
                #camera_thread2 = threading.Thread(target=self.run_observations, args=("arm", 0))
                #camera_thread2.start()
                #self._recieved_data["images"]["navbgr"] = False
                #self._recieved_data["images"]["armbgr"] = False 
            else:
                for cam_source in camera_sources:
                    cam_thread = threading.Thread(target=self.run_all_observations, args=(cam_source, 0))
                    cam_thread.start()
                    camera_threads.append(cam_thread)
                    self._recieved_data["images"][cam_source + "bgr"] = False
                    self._recieved_data["images"][cam_source + "depth"] = False
                #camera_thread1 = threading.Thread(target=self.run_all_observations, args=("nav", 0))
                #camera_thread1.start()
                #camera_thread2 = threading.Thread(target=self.run_all_observations, args=("arm", 0))
                #camera_thread2.start()
                #self._recieved_data["images"]["navbgr"] = False
                #self._recieved_data["images"]["armbgr"] = False
                #self._recieved_data["images"]["navdepth"] = False
                #self._recieved_data["images"]["armdepth"] = False

    def check_timebound(self):
        pass 


    def step(self, actions_dict = None):
        start_time = time.time()

        action_completed_timestamp = None
        action_completed_success = None 
        action_completed_error_message = None 

        if actions_dict is not None:
            # make action request
            all_request = robot_command_pb2.RobotCommandRequest()
            
            actions = actions_dict["action"]
            
            # TODO: assuming... either list of string ("Pass" or "Done", or list of {})
            for a in actions:
                #print("action key: ", a)
                if type(a) is not dict and type(a) is str:
                    all_request.robot_commands.append(robot_command_pb2.RobotCommand(action = a))
                else:
                    if "args" not in a.keys():
                        all_request.robot_commands.append(robot_command_pb2.RobotCommand(action = a["action"], args=None))
                    else:
                        all_request.robot_commands.append(robot_command_pb2.RobotCommand(action = a["action"], args=a["args"]))

            # get response
            response = self.command_stub.MoveRobot(all_request)
            print(actions, " action completed: ", time.time()-start_time) #,response.timestamp, response.success, response.error_status) 
            
            action_completed_timestamp = response.timestamp
            action_completed_success = response.success
            action_completed_error_message = response.error_status #enum to string message
            
            self.robot_last_action["timestamp"] = action_completed_timestamp
            self.robot_last_action["actions"] = actions # actions_dict

            if self.multi_thread:
                self.flag_action_timestamp(action_completed_timestamp)
                #print("action timestamp", action_completed_timestamp)
   

        ## TODO: update state and observation. make sure the timestamp is after ressponse timestamp
        if not self.multi_thread:
            self.get_state(action_completed_timestamp)

            if not self.get_depth:
                for cam_source in self.camera_sources:
                    self.get_observations(cam_source, 0, action_completed_timestamp)
                #self.get_observations("arm", 0, action_completed_timestamp)
                #self.get_observations("nav", 0, action_completed_timestamp)

            ## TODO: passed by config
            else:#if self.get_depth:
                for cam_source in self.camera_sources:
                    self.get_all_observations(cam_source, action_completed_timestamp)
                #self.get_all_observations("arm", action_completed_timestamp)
                #self.get_all_observations("nav", action_completed_timestamp)

            images = self.images 
            robot_state = self.robot_state
            #print("Robot State Arm Lift Pos", self.robot_state["arm"]["lift_m"])
        else: # is multi_thread
            while True:
                if self._recieved_data["robot_state"]:
                    robot_state = self.robot_state
                    break

            while True:
                #print(self._recieved_data["images"].values())
                if all(self._recieved_data["images"].values()):
                    images = self.images
                    break
        
        if PRINT_TIMESTAMPS and action_completed_timestamp :
            print("robot d_timestamp", difference_in_timestamps(robot_state["timestamp"] , action_completed_timestamp))
            print("image nav d_timestamp", difference_in_timestamps(images["nav"]["bgr"]["timestamp"], action_completed_timestamp))
            print("image arm d_timestamp", difference_in_timestamps(images["arm"]["bgr"]["timestamp"], action_completed_timestamp))
            
            if self.get_depth:
                print("image nav d_timestamp", difference_in_timestamps(images["nav"]["depth"]["timestamp"], action_completed_timestamp))
                print("image arm d_timestamp", difference_in_timestamps(images["arm"]["depth"]["timestamp"], action_completed_timestamp))
                
        

        print("[Step] Processing time: ", time.time()-start_time)
        return (robot_state, images), (action_completed_success, action_completed_error_message, action_completed_timestamp)
        
    def flag_action_timestamp(self, timestamp):
        self._action_timestamp = timestamp
        self._recieved_data["robot_state"] = False
        for cam_source in self.camera_sources:
            self._recieved_data["images"][cam_source + "bgr"] = False
            if self.get_depth:
                self._recieved_data["images"][cam_source + "depth"] = False
            

    def run_all_observations(self, image_source_name, pixel_format):
        while True:
            self.get_all_observations(image_source_name)
                

    def run_observations(self, image_source_name, pixel_format):
        while True:
            self.get_observations(image_source_name, pixel_format) #, self._action_timestamp)

    def run_state(self):
        while True:
            self.get_state()#self._action_timestamp)

    def set_obs_shape(self, width, height):
        self.width = width
        self.height = height 


    def get_all_observations(self, image_source_name, lowerbound_timestamp=None):
        #start_time = time.time()
        print("Getting ALL Observations")

        if self.multi_thread: # and self._action_timestamp is None:
            if self._action_timestamp is not None and lowerbound_timestamp is None: 
                lowerbound_timestamp = self._action_timestamp
        

        response = self.obs_stub.GetAllImages(image_pb2.ImageRequest(image_source_name=image_source_name,
                                                                width=self.width,
                                                                height=self.height))
        

        while lowerbound_timestamp is not None and timestamp1_is_bigger_timestamp2(lowerbound_timestamp, response.timestamp):                    
            response = self.obs_stub.GetAllImages(image_pb2.ImageRequest(image_source_name=image_source_name,
                                                                    width=self.width,
                                                                   height=self.height))
            
        self.images[image_source_name][enum_to_string[0]] = {
            "timestamp" : response.timestamp,
            "data": decode_image(response.images[0].data, 0) #self.decode_image(response.data)
        }

        self.images[image_source_name][enum_to_string[1]] = {
            "timestamp" : response.timestamp,
            "data": decode_image(response.images[1].data, 1, width=self.width, height=self.height) #self.decode_image(response.data)
        }

        if self.multi_thread: # and self._action_timestamp is None:
            if self._action_timestamp is not None and timestamp1_is_bigger_timestamp2(self._action_timestamp, response.timestamp):
                return
            
            if self.width != self.images[image_source_name][enum_to_string[0]]["data"].shape[1]:
                return

            self._recieved_data["images"][image_source_name+enum_to_string[0]] = True
            self._recieved_data["images"][image_source_name+enum_to_string[1]] = True
        
        #print("image: ", image_source_name, time.time() -start_time) #response.timestamp,



    def get_observations(self, image_source_name, pixel_format, lowerbound_timestamp=None):
        #start_time = time.time()
        response = self.obs_stub.GetImage(image_pb2.ImageRequest(image_source_name=image_source_name,
                                                                width=self.width,
                                                                height=self.height,
                                                                pixel_format=pixel_format))
        

        while lowerbound_timestamp is not None and timestamp1_is_bigger_timestamp2(lowerbound_timestamp, response.timestamp):                    
            response = self.obs_stub.GetImage(image_pb2.ImageRequest(image_source_name=image_source_name,
                                                                    width=self.width,
                                                                    height=self.height,
                                                                    pixel_format=pixel_format))
            
        self.images[image_source_name][enum_to_string[pixel_format]] = {
            "timestamp" : response.timestamp,
            "data": decode_image(response.data, pixel_format, width=self.width, height=self.height) #self.decode_image(response.data)
        }

        if self.multi_thread: # and self._action_timestamp is None:
            if self._action_timestamp is not None and timestamp1_is_bigger_timestamp2(self._action_timestamp, response.timestamp):
                return
            
            if self.width != self.images[image_source_name][enum_to_string[pixel_format]]["data"].shape[1]:
                return

            self._recieved_data["images"][image_source_name+enum_to_string[pixel_format]] = True

        #print("image: ", image_source_name, enum_to_string[pixel_format], time.time() -start_time) #response.timestamp,



    def get_state(self, lowerbound_timestamp=None):
        # TODO: put config 
        #start_time = time.time()

        response = self.state_stub.GetRobotState(robot_state_pb2.RobotStateRequest(client_name="RobotClient"))
        while lowerbound_timestamp is not None and timestamp1_is_bigger_timestamp2(lowerbound_timestamp, response.timestamp):            
            response = self.state_stub.GetRobotState(robot_state_pb2.RobotStateRequest(client_name="RobotClient"))

        robot_state = {}
        robot_state["timestamp"] = response.timestamp
        robot_state["agent"] = {}
        robot_state["arm"] = {}

        # why agent? base seems more appropriate
        robot_state["agent"]["position"] = {
            "x" : response.base.position.x,
            "y" : response.base.position.y,
            "z" : response.base.position.z
        }
        robot_state["agent"]["rotation"] = {
            "x" : response.base.rotation.x,
            "y" : math.degrees(-response.base.rotation.y)%360,
            "z" : response.base.rotation.z
        }
        robot_state["arm"]["extension_force"] = response.arm.extension_force
        robot_state["arm"]["extension_m"] = response.arm.extension_pos
        robot_state["arm"]["lift_force"] = response.arm.lift_force
        robot_state["arm"]["lift_m"] = response.arm.lift_pos
        robot_state["arm"]["wrist_degrees"] = response.arm.wrist_degree
        robot_state["arm"]["grip_percent"] = response.arm.grip_percent
        #self.robot_state["arm"]["wrist_effort_percent"]
        #self.robot_state["arm"]["grip_effort"]
        
        #end_time = time.time()
        #print("state: ", response.timestamp, end_time-start_time)
        
        self.robot_state = robot_state

        if self.multi_thread: #and self._action_timestamp is None:
            if self._action_timestamp is not None and timestamp1_is_bigger_timestamp2(self._action_timestamp, response.timestamp):
                return
            self._recieved_data["robot_state"] = True 


if __name__ == "__main__":
    robot_client = RobotClient(multi_thread=False)#, width=760, height=480)
    while True:
        robot_client.step()
        if robot_client.images["nav"]["bgr"]["data"] is not None:
            cv2.imshow("image", cv2.cvtColor(decode_image(robot_client.images["nav"]["bgr"]["data"]), cv2.COLOR_BGR2RGB))
            cv2.waitKey(1)