import torch

class TicTacToe:
    def __init__(self, starting_player=1):
        assert starting_player == 1 or starting_player == -1
        self.has_next_move = starting_player

        # 8x8 game of checkers
        self.board = torch.zeros((3, 3))

    def get_valid_actions(self):
        # extracts all game positions for a particular player
        # self.has_next_move will be either 1 or -1
        positions = (self.board == 0).nonzero()
        return [(int(p[0]), int(p[1])) for p in positions]

    def step(self, pos: tuple):
        # draw on position should be a tuple from get_valid_actions
        assert pos in self.get_valid_actions()

        # make move
        row, col = pos
        self.board[row, col] = self.has_next_move 

        # change the player
        self.has_next_move *= -1

    def game_over(self):
        """Returns 1 if p1 wins, -1 if p2 wins, and 0 if game is not over.
           
           Assumes only one player is the winner.
           If the board is something like:
               1 | _ | -1
               1 | _ | -1
               1 | _ | -1
           there may be unexpected results."""
        b = self.board

        def determine_winner(tensor):
            # for each triplet, returns 1 if p1 wins, -1 if p2 wins,
            # and 0 if undetermined

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
