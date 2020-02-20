import torch

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
        self.has_next_move = starting_player

        # 8x8 game of checkers
        self.board = torch.zeros((3, 3))

    def get_valid_actions(self):
        """
        example:
            `game.get_valid_actions()`

        exampleOut:
            `[(0, 0), (0, 1), (1, 1), (2, 1)]`

        description:
            Extracts all board positions where the next player can play.
        """
        positions = (self.board == 0).nonzero()
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
        self.board[row, col] = self.has_next_move 

        # change the player
        self.has_next_move *= -1

    def game_over(self):
        """
        example:
            `game.game_over()`

        description:
            Returns 1 if p1 wins, -1 if p2 wins, and 0 if game is not over.
            Assumes only one player is the winner.
            If the board is something like:
                 1 | _ | -1
                 1 | _ | -1
                 1 | _ | -1
            there may be unexpected results.
        """
        b = self.board

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

        return (
            max(-1,
                min(1,
                    determine_winner(b[:, 0]) +
                    determine_winner(b[:, 1]) +
                    determine_winner(b[:, 2]) +
                    determine_winner(b[0, :]) +
                    determine_winner(b[1, :]) +
                    determine_winner(b[2, :]) +
                    determine_winner(torch.diag(b)) +
                    determine_winner(torch.tensor([b[0, 2], b[1, 1], b[2, 0]]))
                )
            )
        )
