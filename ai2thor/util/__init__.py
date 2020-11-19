import os

# python2.7 compatible makedirs
def makedirs(directory):
    if not os.path.isdir(directory):
        os.makedirs(directory)