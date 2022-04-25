from ai2thor.interact import StdinPrompt
import sys

class NavigationPrompt(StdinPrompt):
    def __init__(
            self,
            default_actions,
            arrow_controls = True
    ):
        self.default_actions = default_actions
        self.counter = 0

        prompt_character_map = {
            "MoveRight": "\u2192",
            "MoveLeft": "\u2190",
            "MoveAhead": "\u2191",
            "MoveBack": "\u2193",
            "LookUp": "\u2191",
            "LookDown": "\u2193",
            "RotateRight": "\u2192",
            "RotateLeft": "\u2190"
        }
        #
        # default_interact_commands = {
        #     "\x1b[C": dict(action="MoveRight", moveMagnitude=0.25),
        #     "\x1b[D": dict(action="MoveLeft", moveMagnitude=0.25),
        #     "\x1b[A": dict(action="MoveAhead", moveMagnitude=0.25),
        #     "\x1b[B": dict(action="MoveBack", moveMagnitude=0.25),
        #     "\x1b[1;2A": dict(action="LookUp"),
        #     "\x1b[1;2B": dict(action="LookDown"),
        #     "i": dict(action="LookUp"),
        #     "k": dict(action="LookDown"),
        #     "l": dict(action="RotateRight"),
        #     "j": dict(action="RotateLeft"),
        #     "\x1b[1;2C": dict(action="RotateRight"),
        #     "\x1b[1;2D": dict(action="RotateLeft"),
        # }

        default_interact_commands = {
            "1": dict(action="MoveAhead", moveMagnitude=0.25),
            "2": dict(action="RotateLeft"),
            "3": dict(action="RotateRight"),
            "4": dict(action="LookUp"),
            "5": dict(action="LookDown"),
            "6": dict(action="End")
        }

        action_set = {a.name for a in default_actions}

        self.default_interact_commands = {
            k: v
            for (k, v) in default_interact_commands.items()
            if v["action"] in action_set
        }

        if not arrow_controls:
            movePrompt = [prompt_character_map[a] for a in ["MoveLeft", "MoveAhead", "MoveRight", "MoveBack"] if a in action_set]
            rotatePrompt = [prompt_character_map[a] for a in ["RotateLeft", "RotateRight", "LookUp", "LookDown"] if
                          a in action_set]

            moveMessage = "Move: {}".format("".join(movePrompt)) if len(movePrompt) > 0 else ""
            lookMessage = "/Look" if any([a for a in action_set if "Look" in a]) else ""
            rotateMessage = "Rotate{}: shift + {}".format(lookMessage, "".join(rotatePrompt)) if len(movePrompt) > 0 else ""

            messages = filter(lambda x: x != "", [moveMessage, rotateMessage])

            self.command_message = u"Enter a Command:\n{} | Quit 'q' or Ctrl-C".format(" | ".join(messages))
        else:
            actions = [a.name for a in default_actions]
            action_map = {v["action"]:k for (k, v) in default_interact_commands.items()}
            self.command_message = u"Enter a Command:\n{} | Quit 'q' or Ctrl-C""".format(
                " | ".join(
                    ["({}) {}".format(action_map[a.name], a.name) for a in default_actions]
                )
            )

    def interact(
            self,
            controller,
            step=True,
            **kwargs
    ):
        if not sys.stdout.isatty():
            raise RuntimeError("controller.interact() must be run from a terminal")

        default_interact_commands = self.default_interact_commands
        interact_commands = default_interact_commands.copy()

        print(self.command_message)

        for a in StdinPrompt.next_interact_command(interact_commands):

            if step:
                event = controller.step(a)
            else:
                event = controller.last_event

            return a["action"]
