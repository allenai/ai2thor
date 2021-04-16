"""
A video controller for ai2thor
Basic example:
from ai2thor.controller import VideoController
with VideoController() as vc:
    vc.play(vc.MoveAhead())
    vc.wait(5)
    vc.play(vc.MoveAhead())
    vc.exportVideo('thor.mp4')
Known issues:
- Multi agent rotations don't work (since TeleportFull breaks when passing in an AgentID)
"""

from ai2thor.controller import Controller
import cv2
import os
from PIL import Image
import math
from math import erf, sqrt


class VideoController(Controller):
    def __init__(
        self,
        cam_rot=dict(x=85, y=225, z=0),
        cam_pos=dict(x=-1.25, y=7.0, z=-1.0),
        cam_fov=60,
        **controller_kwargs,
    ):
        super().__init__(continuous=True, **controller_kwargs)
        self.step(
            action="AddThirdPartyCamera",
            rotation=initial_camera_rotation,
            position=initial_camera_position,
            fieldOfView=initial_camera_fov,
        )

        self.saved_frames = []
        self.ceiling_off = False
        self.initial_cam_rot = cam_rot.copy()
        self.initial_cam_pos = cam_pos.copy()
        self.initial_cam_fov = cam_fov

    def reset(self, scene):
        """Changes the scene and adds a new third party camera to the initial position."""
        super().reset(scene)
        self.step(
            action="AddThirdPartyCamera",
            rotation=self.initial_cam_rot,
            position=self.initial_cam_pos,
            fieldOfView=self.initial_cam_fov,
        )

    def play(self, *action_generators):
        """Apply multiple actions at the same time (e.g., move multiple agents,
        and pan the camera around the scene.

        Examples
        vc.play(vc.moveAhead())
        vc.wait(60)
        vc.play(vc.moveAhead(), vc.orbitCameraAnimation(0, 0, 0))"""
        # action_generators should be a list of generators (e.g., moveAhead(<Params>))
        # this does many transformations at the same time
        while True:
            # execute next actions if available
            next_actions = [next(generator, False) for generator in action_generators]

            # add the frame to the saved frames after all actions execute
            self.saved_frames.append(self.last_event.third_party_camera_frames[0])

            # remove actions with finished iterators
            next_actions = [action for action in next_actions if action != False]

            if not next_actions:
                # exit after all generators have finished
                break

    def _wait(self, frames=60):
        """Yields a generator used in self.wait()"""
        for _ in range(frames):
            yield self.step(action="Pass")

    def wait(self, frames=60):
        """Do absolutely nothing to the agent. Keep the current frame still, as is.

        Params
        - frames (int)=60: The duration of the do nothing action.
          Note: videos are typically 30fps.

        Example: vc.wait(60)"""
        self.play(self._wait(frames))

    def ToggleCeiling(self):
        """Hides the ceiling. This method is greatly preferred over calling
        step(action='ToggleMapView') directly, since it allows for automatic
        ceiling toggles in the future. (e.g., if the camera is above the
        height of the room, toggle off the ceiling, and vice versa."""
        self.ceiling_off = not self.ceiling_off
        return self.step(action="ToggleMapView")

    def _cdf(self, x, std_dev=0.5, mean=0.0):
        """Cumulative distribution function"""
        return (1.0 + erf((x - mean) / sqrt(2.0 * std_dev ** 2))) / 2.0

    def _linear_to_smooth(self, curr_frame, total_frames, std_dev=0.5, min_val=3):
        # start at -3 STD on a normal gaussian, go to 3 STD on gaussian
        # curr frame should be 1 indexed, and end with total_frames
        assert min_val > 0, "Min val should be > 0"

        if curr_frame == total_frames:
            # removes drifting
            return 1

        return self._cdf(
            -min_val + 2 * min_val * (curr_frame / total_frames), std_dev=std_dev
        )

    def _move(self, actionName, moveMagnitude, frames, smoothAnimation, agentId=None):
        """Yields a generator full of move commands to move the agent incrementally.
        Used as a general move command for MoveAhead, MoveRight, MoveLeft, MoveBack."""
        last_moveMag = 0
        for i in range(frames):
            # smoothAnimation = False => linear animation
            if smoothAnimation:
                next_moveMag = (
                    self._linear_to_smooth(i + 1, frames, std_dev=1) * moveMagnitude
                )
                if agentId is None:
                    yield self.step(
                        action=actionName, moveMagnitude=next_moveMag - last_moveMag
                    )
                else:
                    yield self.step(
                        action=actionName,
                        moveMagnitude=next_moveMag - last_moveMag,
                        agentId=agentId,
                    )
                last_moveMag = next_moveMag
            else:
                if agentId is None:
                    yield self.step(
                        action=actionName, moveMagnitude=moveMagnitude / frames
                    )
                else:
                    yield self.step(
                        action=actionName,
                        moveMagnitude=moveMagnitude / frames,
                        agentId=agentId,
                    )

    def _rotate(self, direction, rotateDegrees, frames, smoothAnimation, agentId=None):
        """Yields a generator full of step(action='TeleportFull') commands to rotate the agent incrementally."""
        if agentId is not None:
            raise ValueError("rotations do not yet work with multiple agents")

        # make it work for left and right rotations
        direction = direction.lower()
        assert direction == "left" or direction == "right"
        if direction == "left":
            rotateDegrees *= -1

        # get the initial rotation
        y0 = self.last_event.metadata["agent"]["rotation"]["y"]
        for i in range(frames):
            # keep the position the same
            p = self.last_event.metadata["agent"]["position"]
            if smoothAnimation:
                if agentId is None:
                    yield self.step(
                        action="TeleportFull",
                        rotation=y0
                        + rotateDegrees
                        * self._linear_to_smooth(i + 1, frames, std_dev=1),
                        agentId=agentId,
                        **p,
                    )
                else:
                    yield self.step(
                        action="TeleportFull",
                        rotation=y0
                        + rotateDegrees
                        * self._linear_to_smooth(i + 1, frames, std_dev=1),
                        **p,
                    )
            else:
                if agentId is None:
                    yield self.step(
                        action="TeleportFull",
                        rotation=y0 + rotateDegrees * ((i + 1) / frames),
                        agentId=agentId,
                        **p,
                    )
                else:
                    yield self.step(
                        action="TeleportFull",
                        rotation=y0 + rotateDegrees * ((i + 1) / frames),
                        **p,
                    )

    def MoveAhead(self, moveMagnitude=1, frames=60, smoothAnimation=True, agentId=None):
        return self._move(
            "MoveAhead", moveMagnitude, frames, smoothAnimation, agentId=agentId
        )

    def MoveBack(self, moveMagnitude=1, frames=60, smoothAnimation=True, agentId=None):
        return self._move(
            "MoveBack", moveMagnitude, frames, smoothAnimation, agentId=agentId
        )

    def MoveLeft(self, moveMagnitude=1, frames=60, smoothAnimation=True, agentId=None):
        return self._move(
            "MoveLeft", moveMagnitude, frames, smoothAnimation, agentId=agentId
        )

    def MoveRight(self, moveMagnitude=1, frames=60, smoothAnimation=True, agentId=None):
        return self._move(
            "MoveRight", moveMagnitude, frames, smoothAnimation, agentId=agentId
        )

    def RotateRight(
        self, rotateDegrees=90, frames=60, smoothAnimation=True, agentId=None
    ):
        # do incremental teleporting
        return self._rotate(
            "right", rotateDegrees, frames, smoothAnimation, agentId=agentId
        )

    def RotateLeft(
        self, rotateDegrees=90, frames=60, smoothAnimation=True, agentId=None
    ):
        # do incremental teleporting
        return self._rotate(
            "left", rotateDegrees, frames, smoothAnimation, agentId=agentId
        )

    def OrbitCameraAnimation(
        self,
        centerX,
        centerZ,
        posY,
        dx=6,
        dz=6,
        xAngle=55,
        frames=60,
        orbit_degrees_per_frame=0.5,
    ):
        """Orbits the camera around the scene.

        Example: https://www.youtube.com/watch?v=KcELPpdN770&feature=youtu.be&t=14"""
        degrees = frames * orbit_degrees_per_frame
        rot0 = self.last_event.metadata["thirdPartyCameras"][0]["rotation"][
            "y"
        ]  # starting angle
        for frame in range(frames):
            yAngle = rot0 + degrees * (frame + 1) / frames
            yield self.step(
                action="UpdateThirdPartyCamera",
                thirdPartyCameraId=0,
                rotation={"x": xAngle, "y": yAngle, "z": 0},
                position={
                    "x": centerX - dx * math.sin(math.radians(yAngle)),
                    "y": posY,
                    "z": centerZ - dz * math.cos(math.radians(yAngle)),
                },
            )

    def RelativeCameraAnimation(self, px=0, py=0, pz=0, rx=0, ry=0, rz=0, frames=60):
        """Linear interpolation between the current camera position and rotation
           and the final camera position, given by deltas to the current values.

        Params
        - px (int)=0: x offset from the current camera position.
        - py (int)=0: y offset from the current camera position.
        - pz (int)=0: z offset from the current camera position.
        - rx (int)=0: x offset from the current camera rotation.
        - ry (int)=0: y offset from the current camera rotation.
        - rz (int)=0: z offset from the current camera rotation.
        - frames (int)=60: The duration of the animation.
          Note: videos are typically 30fps."""
        for _ in range(frames):
            cam = self.last_event.metadata["thirdPartyCameras"][0]
            pos, rot = cam["position"], cam["rotation"]
            yield self.step(
                action="UpdateThirdPartyCamera",
                thirdPartyCameraId=0,
                rotation={
                    "x": rot["x"] + rx / frames,
                    "y": rot["y"] + ry / frames,
                    "z": rot["z"] + rz / frames,
                },
                position={
                    "x": pos["x"] + px / frames,
                    "y": pos["y"] + py / frames,
                    "z": pos["z"] + pz / frames,
                },
            )

    def AbsoluteCameraAnimation(
        self,
        px,
        py,
        pz,
        rx,
        ry,
        rz,
        frames=60,
        smartSkybox=True,
        FOVstart=None,
        FOVend=None,
        visibleAgents=True,
    ):
        cam = self.last_event.metadata["thirdPartyCameras"][0]
        p0, r0 = cam["position"], cam["rotation"]

        if smartSkybox:
            # toggles on and off (to give the same final result) to find the height of the ceiling
            event0 = self.step(action="ToggleMapView")
            event1 = self.step(action="ToggleMapView")
            if event0.metadata["actionReturn"]:
                maxY = event0.metadata["actionReturn"]["y"]
            else:
                maxY = event1.metadata["actionReturn"]["y"]

        for i in range(1, frames + 1):
            if self.ceiling_off and maxY > p0["y"] + (py - p0["y"]) / frames * i:
                # turn ceiling on
                self.ToggleCeiling()

            kwargs = {
                "action": "UpdateThirdPartyCamera",
                "thirdPartyCameraId": 0,
                "rotation": {
                    "x": r0["x"] + (rx - r0["x"]) / frames * i,
                    "y": r0["y"] + (ry - r0["y"]) / frames * i,
                    "z": r0["z"] + (rz - r0["z"]) / frames * i,
                },
                "position": {
                    "x": p0["x"] + (px - p0["x"]) / frames * i,
                    "y": p0["y"] + (py - p0["y"]) / frames * i,
                    "z": p0["z"] + (pz - p0["z"]) / frames * i,
                },
            }

            # enables linear animation changes to the camera FOV
            if FOVstart is not None and FOVend is not None:
                kwargs["fieldOfView"] = FOVstart + (FOVend - FOVstart) / frames * i

            if not (smartSkybox and maxY > p0["y"] + (py - p0["y"]) / frames * i):
                kwargs["skyboxColor"] = "black"
            yield self.step(**kwargs)

    def LookUp(self):
        raise NotImplementedError()

    def LookDown(self):
        raise NotImplementedError()

    def Stand(self):
        """Note: have not found an easy way to move the agent in-between
        stand and crouch."""
        raise NotImplementedError()

    def Crouch(self):
        """Note: have not found an easy way to move the agent in-between
        stand and crouch."""
        raise NotImplementedError()

    def export_video(self, path):
        """Merges all the saved frames into a .mp4 video and saves it to `path`"""
        if self.saved_frames:
            path = path if path[:-4] == ".mp4" else path + ".mp4"
            if os.path.exists(path):
                os.remove(path)
            video = cv2.VideoWriter(
                path,
                cv2.VideoWriter_fourcc(*"DIVX"),
                30,
                (self.saved_frames[0].shape[1], self.saved_frames[0].shape[0]),
            )
            for frame in self.saved_frames:
                # assumes that the frames are RGB images. CV2 uses BGR.
                video.write(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
            cv2.destroyAllWindows()
            video.release()

    def export_frames(self, path, file_type=".png"):
        """Exports all of the presently frames to the `path` directory.

        The frames are numbered in sequential order (starting with 0)."""
        for i in range(len(self.saved_frames)):
            p = os.path.join(path, f"{i}.{file_type}")
            if os.path.exists(p):
                os.remove(p)
            Image.fromarray(self.saved_frames[i]).save(p)

    def merge_video(self, other_video_path):
        """Concatenates the frames of `other_video_path` to the presently
        generated video within this class."""
        vidcap = cv2.VideoCapture(other_video_path)
        success, image = vidcap.read()
        i = 0
        while success:
            if i % 2 == 0:
                rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
                self.saved_frames.append(rgb)
            success, image = vidcap.read()
            i += 1
