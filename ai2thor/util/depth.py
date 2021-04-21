import os
import numpy as np

dir_path = os.path.dirname(os.path.realpath(__file__))


def generate_noise_indices(img_size):
    img_size = int(img_size)
    default_size = 300
    corners = np.load(os.path.join(dir_path, "depth_noise.npy"), allow_pickle=True)
    indices = []
    for j, corner in enumerate(corners):
        height_indices = np.array([], dtype=np.int32)
        width_indices = np.array([], dtype=np.int32)

        if img_size != default_size:
            idx = 0 if j <= 1 else len(corner) - 1
            width = corner[idx]
            height = len(corner)

            w_ratio = width / default_size
            h_ratio = height / default_size

            width = int(round(w_ratio * img_size))
            height = int(round(h_ratio * img_size))

            m = (height - 0) / (0 - width)
            b = height
            t = np.array([], dtype=np.int32)
            for y in range(height):
                x = (y - b) / m
                t = np.append(t, int(round(x)))

            t = np.flip(t, 0) if j > 1 else t
            corner = t

        for i, c in enumerate(corner):

            offset = 0
            i_offset = 0
            if j % 2 != 0:
                offset = img_size - c
            if j > 1:
                i_offset = img_size - len(corner)
            x = np.repeat(i_offset + i, c)
            height_indices = np.concatenate((height_indices, x))
            y = np.array(range(offset, offset + c))
            width_indices = np.concatenate((width_indices, y))

            indices.append(
                (
                    np.array(height_indices, dtype=np.int32),
                    np.array(width_indices, dtype=np.int32),
                )
            )

    return indices


def apply_real_noise(depth_arr, size, indices=None):
    """
    Applies noise to depth image to look more similar to the real depth camera
    :param depth_arr: numpy square 2D array representing the depth
    :param size: square size of array
    :param indices: cached indices where noise is going to be applied, if None they get calculated
                    here based on the image size.
    :return:
    """
    if indices is None:
        indices = generate_noise_indices(size)
    for index_arr in indices:
        depth_arr[index_arr] = 0.0
    return depth_arr
