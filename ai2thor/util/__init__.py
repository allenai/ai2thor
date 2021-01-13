import os
import random
import time


# python2.7 compatible makedirs
def makedirs(directory):
    if not os.path.isdir(directory):
        os.makedirs(directory)


def atomic_write(path, data):
    tmp_path = '-'.join([path, str(time.time()), str(random.random())])
    mode = 'w'

    if type(data) is bytes:
        mode = 'wb'

    with open(tmp_path, mode) as f:
        f.write(data)
    os.rename(tmp_path, path)
