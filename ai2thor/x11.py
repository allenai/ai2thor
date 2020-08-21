# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.x11

X11 wrapper for Cython module that interacts with the Display to capture Unity's game window
"""
import glob
from pprint import pprint
import atexit
import ai2thor.controller
import json
from itertools import product
import numpy as np
import fcntl
import random
import signal
import os


LOADED = False
try:
    # x11 won't build on osx
    # XXX CHECK IF DISPLAY is SET!!
    import ai2thor._x11
    LOADED = True
except ImportError:
    pass


class X11:

    def __init__(self, width, height):

        self._x11 = ai2thor._x11.X11()
        launch_lock_file = os.path.join(os.environ['HOME'], '.ai2thor/lock/launch.lock')

        os.makedirs(os.path.dirname(launch_lock_file), exist_ok=True)
        lock_f = open(launch_lock_file, "w")

        # We will need separate window ids for each camera in the future
        self.window_id = None
        self._x11.setup(width, height)
        atexit.register(lambda: self._x11.cleanup())

    def __del__(self):
        print("del called in x11")

    def get_candidate_windows(self, target_width, target_height):
        candidates = []
        for w in self.all_windows():
            print(w)
            width, height, x, y = self._x11.get_geometry(w)
            print("candidate geometry %s %s" % (width, height))
            if width == target_width and height == target_height:
                candidates.append(w)
        return candidates

    def all_windows(self, root_window=0, depth=0):
        window_ids = []
        if depth == 0:
            pass
            #print("")
        for w in self._x11.list_all_windows(root_window):
            #print(" " * (depth * 2) +  str(w) )
            window_ids.append(w)
            window_ids.extend(self.all_windows(w, depth = depth+1))
    
        return window_ids

    def check_window_contents(self, window_id, event):
        return np.array_equal(self._x11.get_xshm_image(window_id, 300, 300), event.cv2img)

    def find_controller_window(self, event, width, height):
        candidates = self.get_candidate_windows(width, height)

        for c in candidates:

            if self.check_window_contents(c, event):
                self.window_id = c
                #print("candidate match %s" % c)
                return True
        return False

    def window_dimensions(self, window_id):
        width = height = x = y = 0
        while window_id:
            #print("window id %s"  % window_id)
            width, height, x, y = self._x11.get_geometry(window_id)
            #print("width %s" % width)
            #print("height %s" % height)
            #print("x %s" % x)
            #print("y %s" % y)
            window_id = self._x11.parent_window(window_id)
        return width, height, x, y

    def continue_procs(self, running):
        for p in running:
            os.kill(p['python'], signal.SIGCONT)

    def stop_procs(self, running):
        for p in running:
            os.kill(p['python'], signal.SIGSTOP)

    def store_proc(self, unity_pid, windows):
        path = os.path.join(os.path.join(os.environ['HOME'], '.ai2thor/run/%s.json') % os.getpid())
        with open(path, "w") as f:
            f.write(json.dumps(dict(python=os.getpid(), windows=[], unity=unity_pid)))

    def running_procs(self):
        run_dir = os.path.join(os.environ['HOME'], '.ai2thor/run')
        os.makedirs(run_dir, exist_ok=True)
        running = []
        for g in glob.glob(os.path.join(run_dir, "*.json")):
            with open(g) as f:
                j = json.loads(f.read())

            if ai2thor.controller.process_alive(j['python']):
                running.append(j)
            else:
                print("unlinking %s" % g)
                os.unlink(g)

        return running

    def move_window_free_space(self, window_id):
        target_width, target_height, _, _ = self.window_dimensions(window_id)
        root_width, root_height, _, _ = self._x11.get_root_geometry()

        existing_windows = self.all_windows()
        dims = {}
        for w in existing_windows:
            dims[w] = self.window_dimensions(w)

        print("moving here")
        pprint(dims)
        print(existing_windows)
        found_x = -1
        found_y = -1
        other_windows = set(existing_windows) - set([window_id])
        # XXX add relative x, relative y
        window_buffer = 2
        for root_x, root_y in product(range(0, (root_width - target_width)), range(0, (root_height - target_height))):
            overlap = False

            #print("root x %s" % root_x)
            #print("root y %s" % root_y)
            for w in other_windows:
                width, height, x, y = dims[w]
                width += (window_buffer * 2)
                height += (window_buffer * 2)
                x -= window_buffer
                y -= window_buffer
                x_overlap = False
                y_overlap = False
                if (root_x < (width + x) and (root_x + target_width) > x):
                    x_overlap = True
                if (root_y < (height + y) and (root_y + target_height) > y):
                    y_overlap = True

                if x_overlap and y_overlap:
                    overlap = True

            if not overlap:
                found_x = root_x
                found_y = root_y
                print("found location x:%s y:%s" % (root_x, root_y))
                break
        if found_x >= 0 and found_y >= 0:
            print("moving window to %s, %s" % (found_x, found_y))
            self._x11.move_window(window_id, found_x, found_y)
        else:
            raise Exception("no free space found, cannot launch")
                               
    def cleanup(self):
        self._x11.cleanup()

    def bind_window(self):

        fcntl.flock(lock_f, fcntl.LOCK_EX)
        running = running_procs()
        stop_procs(running)
        #controller = ai2thor.controller.Controller()
        #candidates = get_candidate_windows(xshm, 300, 300)
        #print("candidates")
        #print(candidates)
        #controller_window = find_controller_window(controller, xshm, candidates)
        #print("HERE MOVING TO FREE")
        self.move_window_free_space(0)
        # XXX validate
        continue_procs(running)
        fcntl.flock(lock_f, fcntl.LOCK_UN)
        xshm.cleanup()

