import ai2thor.controller
import cv2
import os
from PIL import Image

# for smooth non-linear animations
import scipy.stats as st

from queue import Queue

import math

# store a queue for future camera animation updates so they can be interleaved with changing the 

# create many threads that work at the same time

# need transparent skybox/background

# also need agentType stochastic to work

# reset removes third party cameras


# save third party frames and agent frames and then give an option of which, or both, to render.

class VideoController(ai2thor.controller.Controller):
	def __init__(self, orbit_degrees_per_frame=0.5, **controller_kwargs):
		super().__init__(continuous=True, **controller_kwargs)
		# super().__init__(**controller_kwargs)

		# add 3rd party camera... todo: make adding a 3rd party camera an action
		self.step(
			action='AddThirdPartyCamera', 
			rotation=dict(x=85, y=225, z=0), 
			position=dict(x=-1.25, y=7.0, z=-1.0),
			fieldOfView=60)

		self.saved_frames = []
		self.ceiling_off = False
		self.orbit_degrees_per_frame = orbit_degrees_per_frame

	def transform(self, *action_generators: list):
		# action_generators should be a list of generators (e.g., moveAhead(<Params>))
		# this does many transformations at the same time
		
		while True:
			# execute next actions if available
			# if self.ceiling_off: self.step(action='ToggleMapView')
			next_actions = [next(generator, False) for generator in action_generators]
			# if self.ceiling_off: self.step(action='ToggleMapView')

			# add the frame to the saved frames after all actions execute
			self.saved_frames.append(self.last_event.third_party_camera_frames[0])

			# remove actions with finished iterators
			next_actions = [action for action in next_actions if action != False]

			if not next_actions:
				# exit after all generators have finished
				break

	def toggleCeiling(self):
		self.ceiling_off = not self.ceiling_off
		return self.step(action='ToggleMapView')

	def _linear_to_smooth(self, curr_frame, total_frames, std=0.5, min_val=3):
		# start at -3 STD on a normal gaussian, to to 3 STD on gaussian
		# curr frame should be 1 indexed, and end with total_frames
		assert min_val > 0, "Min val should be > 0"

		distribution = st.norm(0, std)

		if curr_frame == total_frames:
			# removes drifting
			return 1

		return distribution.cdf(- min_val + 2 * min_val * (curr_frame / total_frames))

	def _move(self, actionName, moveMagnitude, frames, smoothAnimation):
		"""General move command for MoveAhead, MoveRight, MoveLeft, MoveBack."""
		last_moveMag = 0
		for i in range(frames):
			# smoothAnimation = False => linear animation
			if smoothAnimation:
				next_moveMag = self._linear_to_smooth(i + 1, frames, std=1) * moveMagnitude
				yield self.step(action=actionName, moveMagnitude=next_moveMag - last_moveMag)
				last_moveMag = next_moveMag
			else:
				yield self.step(action=actionName, moveMagnitude=moveMagnitude / frames)

	def _rotate(self, direction, rotateDegrees, frames, smoothAnimation):
		# make it work for left and right rotations
		direction = direction.lower()
		assert direction == 'left' or direction == 'right'
		if direction == 'left': rotateDegrees *= -1

		# get the initial rotation
		y0 = self.last_event.metadata['agent']['rotation']['y']
		for i in range(frames):
			# keep the position the same
			p = self.last_event.metadata['agent']['position']
			if smoothAnimation:
				yield self.step(action='TeleportFull', rotation=y0 + rotateDegrees * self._linear_to_smooth(i + 1, frames, std=1), **p)
			else:
				yield self.step(action='TeleportFull', rotation=y0 + rotateDegrees * ((i + 1) / frames), **p)

	def moveAhead(self, moveMagnitude=1, frames=60, smoothAnimation=True):
		return self._move('MoveAhead', moveMagnitude, frames, smoothAnimation)

	def moveBack(self, moveMagnitude=1, frames=60, smoothAnimation=True):
		return self._move('MoveBack', moveMagnitude, frames, smoothAnimation)

	def moveLeft(self, moveMagnitude=1, frames=60, smoothAnimation=True):
		return self._move('MoveLeft', moveMagnitude, frames, smoothAnimation)

	def moveRight(self, moveMagnitude=1, frames=60, smoothAnimation=True):
		return self._move('MoveRight', moveMagnitude, frames, smoothAnimation)

	def rotateRight(self, rotateDegrees=90, frames=60, smoothAnimation=True):
		# do incremental teleporting
		return self._rotate('right', rotateDegrees, frames, smoothAnimation)

	def rotateLeft(self, rotateDegrees=90, frames=60, smoothAnimation=True):
		# do incremental teleporting
		return self._rotate('left', rotateDegrees, frames, smoothAnimation)

	def Pass(self, frames=60):
		for _ in range(frames):
			yield self.step(action='Pass')

	def orbitCameraAnimation(self, centerX, centerZ, posY, dx=6, dz=6, xAngle=55, frames=60):
		degrees = frames * self.orbit_degrees_per_frame
		rot0 = self.last_event.metadata['thirdPartyCameras'][0]['rotation']['y'] # starting angle
		for frame in range(frames):
			yAngle = rot0 + degrees * (frame + 1) / frames
			yield self.step(action='UpdateThirdPartyCamera',
							thirdPartyCameraId=0,
							rotation={'x': xAngle,
									  'y': yAngle,
									  'z': 0},
							position={'x': centerX - dx * math.sin(math.radians(yAngle)),
									  'y': posY,
									  'z': centerZ - dz * math.cos(math.radians(yAngle))})

	def relativeCameraAnimation(self, px=0, py=0, pz=0, rx=0, ry=0, rz=0, frames=60):
		"""px: position x, rx: rotation x"""
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

	def absoluteCameraAnimation(self, px=None, py=None, pz=None, rx=None, ry=None, rz=None, frames=60, smartSkybox=True):
		cam = self.last_event.metadata['thirdPartyCameras'][0]
		p0, r0 = cam['position'], cam['rotation']

		if smartSkybox:
			e0 = self.step(action='ToggleMapView')
			e1 = self.step(action='ToggleMapView')
			if e0.metadata['actionReturn']:
				maxY = e0.metadata['actionReturn']['y']
			else:
				maxY = e1.metadata['actionReturn']['y']
		# makes math easier
		if not px: px = 0
		if not py: py = 0
		if not pz: pz = 0
		if not rx: rx = 0
		if not ry: ry = 0
		if not rz: rz = 0

		for i in range(1, frames + 1):
			if self.ceiling_off and maxY > p0['y'] + (py - p0['y']) / frames * i:
				print('toggleCeiling!')
				# turn ceiling on
				self.toggleCeiling()
			yield self.step(action='UpdateThirdPartyCamera',
							thirdPartyCameraId=0,
							rotation={'x': r0['x'] + (rx - r0['x']) / frames * i,
									  'y': r0['y'] + (ry - r0['y']) / frames * i,
									  'z': r0['z'] + (rz - r0['z']) / frames * i},
							position={'x': p0['x'] + (px - p0['x']) / frames * i,
									  'y': p0['y'] + (py - p0['y']) / frames * i,
									  'z': p0['z'] + (pz - p0['z']) / frames * i},
							showSkybox=smartSkybox and maxY > p0['y'] + (py - p0['y']) / frames * i)

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
			print((self.saved_frames[0].shape[1], self.saved_frames[0].shape[0]))
			# 
			video = cv2.VideoWriter(path, cv2.VideoWriter_fourcc(*'DIVX'), 30, (self.saved_frames[0].shape[1], self.saved_frames[0].shape[0]))
			for i, frame in enumerate(self.saved_frames):
				print(i, end=', ')
				# assumes that the frames are RGB images. CV2 uses BGR.
				video.write(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
			cv2.destroyAllWindows()
			video.release()
			print('done')

	def exportFrames(self, path):
		# path = path if path[:-4] == '.jpg' else path + '.jpg'
		for i in range(len(self.saved_frames)):
			p = os.path.join(path, f'{i}.jpg')
			if os.path.exists(p):
				os.remove(p)
			Image.fromarray(self.saved_frames[i]).save(p)
		print('done')

	def mergeVideo(self, otherVideoPath):
		import cv2
		vidcap = cv2.VideoCapture('../Place_Kettle.mp4')
		success, image = vidcap.read()
		i = 0
		while success:
			if i % 2 == 0:
				rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
				self.saved_frames.append(rgb)
			success, image = vidcap.read()
			i += 1
