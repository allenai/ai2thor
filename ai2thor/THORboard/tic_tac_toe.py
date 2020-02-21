import torch
import numpy as np

class TicTacToe:
    def __init__(self, starting_player=1):
        """
        example:
            ```from ai2thor.THORboard import TicTacToe
            game = TicTacToe()```

        description:
            This module consists of the functionality to play a standard game
            of TicTacToe 

        params:
            `starting_player` | int | Takes on the values of either 0 or 1 and
                indicates which player has the starting move. | 1
        """
        assert starting_player == 1 or starting_player == -1
        self._has_next_move = starting_player

        # 8x8 game of checkers
        self._board = torch.zeros((3, 3))

    @property
    def board(self):
        """
        example:
            game.board

        description:
            Returns a 3x3 PyTorch tensor of the current game board. Note:
            1 denotes player 1 has played in a position, -1 denotes player 2 has
            played in a position, and 0 denotes that the spot is empty.
        """
        return self._board

    @property
    def has_next_move(self):
        """
        example:
            game.has_next_move

        description:
            Returns 1 or -1, depending on whose turn it is to play next on the
            board.
        """
        return self._has_next_move

    def get_valid_actions(self):
        """
        example:
            `game.get_valid_actions()`

        exampleOut:
            `[(0, 0), (0, 1), (1, 1), (2, 1)]`

        description:
            Extracts all board positions where the next player can play.
        """
        positions = (self._board == 0).nonzero()
        return [(int(p[0]), int(p[1])) for p in positions]

    def step(self, play_on: tuple):
        """
        example:
            `game.step(play_on=(0, 1))`

        description:
            Throws a ValueError if the given position is not on the board or
            has already been played on.

        
        """
        if play_on not in self.get_valid_actions():
            raise ValueError('Invalid position')

        # make move
        row, col = play_on
        self._board[row, col] = self._has_next_move 

        # change the player
        self._has_next_move *= -1

    def game_over(self):
        """
        example:
            `game.game_over()`

        description:
            Returns 1 if p1 wins, -1 if p2 wins, and 0 if game is not over.
            Raises an exception if there are multiple winners on the board,
            e.g.,:
                 1 | _ | -1
                 1 | _ | -1
                 1 | _ | -1
        """
        b = self._board

        def determine_winner(tensor):
            """
                For each triplet, returns 1 if p1 wins, -1 if p2 wins, and 0
                if undetermined.
            """
            # game not over
            if abs(sum(tensor)) != 3:
                return 0

            # game over
            if np.sign(sum(tensor)) > 0:
                return 1
            else:
                return -1

        tensor_results = [
            determine_winner(b[:, 0]),
            determine_winner(b[:, 1]),
            determine_winner(b[:, 2]),
            determine_winner(b[0, :]),
            determine_winner(b[1, :]),
            determine_winner(b[2, :]),
            determine_winner(torch.diag(b)),
            determine_winner(torch.tensor([b[0, 2], b[1, 1], b[2, 0]]))
        ]

        # rows of three where there would be a winner
        nonzero_results = [result for result in tensor_results if result != 0]
        if nonzero_results:
            # asserts that both players are not winners on the board
            if not all(result == nonzero_results[0] for result in nonzero_results):
                raise Exception('Game contains multiple winners.')
            else:
                return nonzero_results[0]
        else:
            return 0
