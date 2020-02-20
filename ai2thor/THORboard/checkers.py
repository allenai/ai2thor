# offline (non-GUI) implementation of checkers
import torch
from collections import defaultdict

# TODO
# - Make kings in capital letters

class Checkers:
    def __init__(self, starting_player=1):
        assert starting_player == 1 or starting_player == -1
        self.has_next_move = starting_player

        # 8x8 game of checkers
        self.board = torch.tensor([0 for _ in range(64)]).reshape(8, 8)

        # sets the starting positions of each game piece
        for row in {0, 2, 6}:
            for col in range(0, 8, 2):
                self.board[row, col] = 1 if row > 4 else -1

        for row in {1, 5, 7}:
            for col in range(1, 8, 2):
                self.board[row, col] = 1 if row > 4 else -1

    def get_valid_actions(self):
        # extracts all game positions for a particular player
        # self.has_next_move will be either 1 or -1
        text_positions = (self.board * self.has_next_move > 0).nonzero()
        out = defaultdict(list)

        for row, col in test_positions:
            if abs(self.board[row, col]) > 1:
                # king checker, can move in either direction
                raise NotImplementedError()
            else:
                if col != 0:
                    # can move left by 1
                    if self.board[row - 1 * self.has_next_move, col - 1] == 0:
                        out[(row, col)].append((row - 1 * self.has_next_move, col - 1))
                    if col != 1:
                        # test if can jump left
                        if self.board[row - 1 * self.has_next_move, col - 1] == -1 and self.board[row - 2 * self.has_next_move, col - 2] == 0:
                            out[(row, col)].append((row - 2 * self.has_next_move, col - 2))
                elif col != 7:
                    if self.board[row - 1 * self.has_next_move, col + 1] == 0:
                        out[(row, col)].append((row - 1 * self.has_next_move, col + 1))
                    if col != 6:
                        # test if can jump left -- todo: make recursive
                        if self.board[row - 1 * self.has_next_move, col + 1] == -1 and self.board[row - 2 * self.has_next_move, col + 2] == 0:
                            out[(row, col)].append((row - 2 * self.has_next_move, col + 2))
                
        self.has_next_move *= -1

    def get_board(self):
        return self.board