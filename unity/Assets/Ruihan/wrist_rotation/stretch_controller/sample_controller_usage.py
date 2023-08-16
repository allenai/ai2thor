import random

import prior
from matplotlib import pyplot as plt

from stretch_controller import StretchController
from stretch_initialization_utils import STRETCH_ENV_ARGS, ALL_STRETCH_ACTIONS

if __name__ == '__main__':
    dataset = prior.load_dataset("procthor-10k")["train"]
    controller = StretchController(**STRETCH_ENV_ARGS, scene=dataset[0])
    plt.imshow(controller.navigation_camera)
    plt.imshow(controller.manipulation_camera)


    for i in range(1000):
        controller.agent_step(random.choice(ALL_STRETCH_ACTIONS))
        plt.imshow(controller.navigation_camera)
        plt.imshow(controller.manipulation_camera)
        plt.show(block=False)
        plt.pause(0.001)
