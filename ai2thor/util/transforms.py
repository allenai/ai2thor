import numpy as np

REAL_2_SIM_TRANSFORM = np.array(
    [[1.00854301, -0.0111386, 0.51920809], [0.00316833, 0.97336625, -1.15532594]]
)


def transform_real_2_sim(real_position):
    """
    Transforms a position from the 'real' coordinate system to the 'sim' coordinate system.
    :param real_position: dictionary with 'x', 'y' and 'z' keys to floating point values
    :return: position in sim space as dictionary with 'x', 'y' and 'z' keys to floating point values
    """

    real_pos = np.array([real_position["x"], real_position["y"], 1])

    sim_pos_np = np.dot(REAL_2_SIM_TRANSFORM, real_pos)

    sim_pos = {"x": sim_pos_np[0], "y": 0.9010001, "z": sim_pos_np[1]}

    return sim_pos
