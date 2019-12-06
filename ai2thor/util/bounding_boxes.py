import numpy as np


def get_corners(bounds):
    """
    :param bounds: axis aligned bounding box, object with center and size vectors.
                   see: ServerMetadata.<name of variable>
    :return: returns an 8 x 3 matrix as numpy array where every row is a corner of the box.
    """
    return np.array([
        [
            bounds['center']['x'] + bounds['size']['x'] / 2,
            bounds['center']['y'] + bounds['size']['y'] / 2,
            bounds['center']['z'] + bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] + bounds['size']['x'] / 2,
            bounds['center']['y'] + bounds['size']['y'] / 2,
            bounds['center']['z'] - bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] + bounds['size']['x'] / 2,
            bounds['center']['y'] - bounds['size']['y'] / 2,
            bounds['center']['z'] + bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] + bounds['size']['x'] / 2,
            bounds['center']['y'] - bounds['size']['y'] / 2,
            bounds['center']['z'] - bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] - bounds['size']['x'] / 2,
            bounds['center']['y'] + bounds['size']['y'] / 2,
            bounds['center']['z'] + bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] - bounds['size']['x'] / 2,
            bounds['center']['y'] + bounds['size']['y'] / 2,
            bounds['center']['z'] - bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] - bounds['size']['x'] / 2,
            bounds['center']['y'] - bounds['size']['y'] / 2,
            bounds['center']['z'] + bounds['size']['z'] / 2
        ],
        [
            bounds['center']['x'] - bounds['size']['x'] / 2,
            bounds['center']['y'] - bounds['size']['y'] / 2,
            bounds['center']['z'] - bounds['size']['z'] / 2
        ]
    
    ])
