# stretch-controller

To test a controller:

```commandline
python sample_controller_usage.py
```

Basic Usage:

```python
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

    for i in range(100):
        controller.agent_step(random.choice(ALL_STRETCH_ACTIONS))

```