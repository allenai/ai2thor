import ai2thor.controller
import cv2
import os

from queue import Queue

# store a queue for future camera animation updates so they can be interleaved with changing the 

# create many threads that work at the same time

class VideoController(ai2thor.controller.Controller):
	def __init__(self, **controller_kwargs):
		super().__init__(**controller_kwargs)
		# controller_kwargs['agentType'] = 'stochastic'
		# super().__init__(**controller_kwargs)

		# add 3rd party camera
		self.step(
			action='AddThirdPartyCamera', 
			rotation=dict(x=0, y=90, z=0), 
			position=dict(x=-1.25, y=4.0, z=-1.0),
			fieldOfView=60)

		self.saved_frames = []
		self.upcoming_camera_animations = Queue()

	def vstep(self, **step_kwargs):

		# update the camera
		if not self.upcoming_camera_animations.empty():
			next_cam_anim_kwargs = self.upcoming_camera_animations.get()
			self.step(next_cam_anim_kwargs)
		pass

	def moveAhead(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			event = self.step(action='MoveAhead', moveMagnitude=moveMagnitude / frames)
			self.saved_frames.append(event.third_party_camera_frames[0])

	def moveBack(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			event = self.step(action='MoveBack', moveMagnitude=moveMagnitude / frames)
			self.saved_frames.append(event.third_party_camera_frames[0])

	def moveLeft(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			event = self.step(action='MoveLeft', moveMagnitude=moveMagnitude / frames)
			self.saved_frames.append(event.third_party_camera_frames[0])

	def moveRight(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			event = self.step(action='MoveRight', moveMagnitude=moveMagnitude / frames)
			self.saved_frames.append(event.third_party_camera_frames[0])

	def rotateRight(self, rotateDegrees=90, frames=60):
		# do teleporting
		pass

	def rotateLeft(self, rotateDegrees=90, frames=60):
		# do teleporting
		pass

	def Pass(self, frames=60):
		for _ in range(frames):
			event = self.step(action='Pass')
			self.saved_frames.append(event.third_party_camera_frames[0])

	def relativeCameraAnimation(self, px=0, py=0, pz=0, rx=0, ry=0, rz=0, frames=60):
		for _ in range(frames):
			cam = self.last_event.metadata['thirdPartyCameras'][0]
			pos, rot = cam['position'], cam['rotation']
			event = self.step(action='UpdateThirdPartyCamera',
					  		  thirdPartyCameraId=0,
					  		  rotation={'x': rot['x'] + rx / frames,
					  		  			'y': rot['y'] + ry / frames,
					  		  			'z': rot['z'] + rz / frames},
					  		  position={'x': pos['x'] + px / frames,
					  			        'y': pos['y'] + py / frames,
					  			        'z': pos['z'] + pz / frames})
			self.saved_frames.append(event.third_party_camera_frames[0])

	def absoluteCameraAnimation(self):
		pass

	def LookUp(self):
		pass

	def LookDown(self):
		pass

	def FocusOnPoint(self):
		pass

	def exportVideo(self, path):
		if self.saved_frames:
			path = path if path[:-4] == '.mp4' else path + '.mp4'
			if os.path.exists(path):
				os.remove(path)
			video = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*'avc1'), 30, (300, 300))
			for frame in self.saved_frames:
				print('i', end=', ')
				video.write(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
			video.release()
			print('done')
