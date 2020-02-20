# offline (non-GUI) implementation of checkers
import numpy as np

# TODO
# - Make kings in capital letters

class Checkers:
	def __init__(self, starting_player='x'):
		# prob_player_starts is the probability that the player starts
		self.game_board = np.array([' ' for i in range(64)]).reshape(8, 8)

		# sets the starting positions of each game piece
		for row in {0, 2, 6}:
			for col in range(0, 8, 2):
				self.game_board[row, col] = 'o' if row > 4 else 'x'

		for row in {1, 5, 7}:
			for col in range(1, 8, 2):
				self.game_board[row, col] = 'o' if row > 4 else 'x'

	def get_(self):
		pass

	def is_king(self, row, col):
		# row and col are 0-indexed



	def get_valid_actions(self, player='white'):
		# Returns a list of all the valid actions, given the games current state
		raise NotImplementedError()

		# should return a nparray of np arrays (inner length is 2 -> (current position) -> (final position) )

	def get_board(self):
		return self.game_board
