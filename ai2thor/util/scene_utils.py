import re


def scene_names_key_func(scene_name):
    """
    Key function for sorting scenes with the naming convention that was used
    """
    m = re.search("FloorPlan[_]?([a-zA-Z\-]*)([0-9]+)_?([0-9]+)?.*$", scene_name)
    last_val = m.group(3) if m.group(3) is not None else -1
    return m.group(1), int(m.group(2)), int(last_val)
