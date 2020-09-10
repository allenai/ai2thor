import os
def process_alive(pid):
    """
    Use kill(0) to determine if pid is alive
    :param pid: process id
    :rtype: bool
    """
    try:
        os.kill(pid, 0)
    except OSError:
        return False

    return True

