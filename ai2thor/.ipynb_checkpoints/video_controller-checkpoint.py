import ai2thor.controller
import cv2
import os

from queue import Queue

# store a queue for future camera animation updates so they can be interleaved with changing the 

# create many threads that work at the same time

# need transparent skybox/background

# also need agentType stochastic to work

# spawn in objects (talk to Luca?)

class VideoController(ai2thor.controller.Controller):
	def __init__(self, **controller_kwargs):
		super().__init__(**controller_kwargs)
		# super().__init__(**controller_kwargs)

		# add 3rd party camera... todo: make adding a 3rd party camera an action
		self.step(
			action='AddThirdPartyCamera', 
			rotation=dict(x=0, y=90, z=0), 
			position=dict(x=-1.25, y=4.0, z=-1.0),
			fieldOfView=60)

		self.saved_frames = []

	def transform(self, *action_generators: list):
		# action_generators should be a list of generators (e.g., moveAhead(<Params>))
		# this does many transformations at the same time
		
		while True:
			# execute next actions if available
			next_actions = [next(generator, False) for generator in action_generators]

			# add the frame to the saved frames after all actions execute
			self.saved_frames.append(self.last_event.third_party_camera_frames[0])

			# remove actions with finished iterators
			next_actions = [action for action in next_actions if action != False]
			print('|', end='')
			if not next_actions:
				# all generators have finished
				break
		print()

	def moveAhead(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			yield self.step(action='MoveAhead', moveMagnitude=moveMagnitude / frames)

	def moveBack(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			yield self.step(action='MoveBack', moveMagnitude=moveMagnitude / frames)

	def moveLeft(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			yield self.step(action='MoveLeft', moveMagnitude=moveMagnitude / frames)

	def moveRight(self, moveMagnitude=1, frames=60):
		for _ in range(frames):
			yield self.step(action='MoveRight', moveMagnitude=moveMagnitude / frames)

	def rotateRight(self, rotateDegrees=90, frames=60):
		# do incremental teleporting
		pass

	def rotateLeft(self, rotateDegrees=90, frames=60):
		# do incremental teleporting
		pass

	def Pass(self, frames=60):
		for _ in range(frames):
			yield self.step(action='Pass')

	def relativeCameraAnimation(self, px=0, py=0, pz=0, rx=0, ry=0, rz=0, frames=60):
		for _ in range(frames):
			cam = self.last_event.metadata['thirdPartyCameras'][0]
			pos, rot = cam['position'], cam['rotation']
			yield self.step(action='UpdateThirdPartyCamera',
							thirdPartyCameraId=0,
							rotation={'x': rot['x'] + rx / frames,
									  'y': rot['y'] + ry / frames,
									  'z': rot['z'] + rz / frames},
							position={'x': pos['x'] + px / frames,
									  'y': pos['y'] + py / frames,
									  'z': pos['z'] + pz / frames})

	def absoluteCameraAnimation(self):
		pass

	def LookUp(self):
		pass

	def LookDown(self):
		pass

	def FocusOnPoint(self):
		pass

	def stand(self):
		pass

	def crouch(self):
		pass

	def exportVideo(self, path):
		# merges all the saved frames into a mp4 video and saves it
		if self.saved_frames:
			path = path if path[:-4] == '.mp4' else path + '.mp4'
			if os.path.exists(path):
				os.remove(path)
			# sets the size of the video to (300, 300) -- TODO: make based on saved_frames[0].size
			video = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*'avc1'), 30, (self.saved_frames[0].shape[1], self.saved_frames[0].shape[0]))
			for i, frame in enumerate(self.saved_frames):
				print(i, end=', ')
				# assumes that the frames are RGB images. CV2 uses BGR.
				video.write(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
			video.release()
			print('done')
