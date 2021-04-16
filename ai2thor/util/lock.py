import os
from platform import system

if system() == "Windows":

    class fcntl:
        LOCK_UN = 0
        LOCK_SH = 0
        LOCK_NB = 0
        LOCK_EX = 0

        @staticmethod
        def fcntl(fd, op, arg=0):
            return 0

        def ioctl(fd, op, arg=0, mutable_flag=True):
            return 0 if mutable_flag else ""

        def flock(fd, op):
            return

        def lockf(fd, operation, length=0, start=0, whence=0):
            return


else:
    import fcntl


class Lock:
    def __init__(self, target, mode):
        self._lock_file_path = target + ".lock"
        self._lock_file = os.open(self._lock_file_path, os.O_RDWR | os.O_CREAT)
        self.mode = mode

    def lock(self):
        fcntl.lockf(self._lock_file, self.mode)

    def unlock(self):
        fcntl.lockf(self._lock_file, fcntl.LOCK_UN)
        os.close(self._lock_file)

    def unlink(self):
        os.unlink(self._lock_file_path)

    def __enter__(self):
        self.lock()

    def __exit__(self, exception_type, exception_value, exception_traceback):
        self.unlock()


class LockEx(Lock):
    def __init__(self, target, blocking=True):
        mode = fcntl.LOCK_EX
        if not blocking:
            mode |= fcntl.LOCK_NB

        super().__init__(target, mode)


class LockSh(Lock):
    def __init__(self, target):
        super().__init__(target, fcntl.LOCK_SH)
