import os
from pathlib import Path

TESTS_DIR = os.path.abspath(os.path.dirname(Path(__file__)))
TESTS_DATA_DIR = os.path.join(TESTS_DIR, "data")
