#!/usr/bin/env python

"""
Script that maintains the Private directory checkout - intended to be run 
immediately after switching branches in the parent ai2thor project
"""
import subprocess
import os
private_repo_url = "https://github.com/allenai/ai2thor-private"
base_dir = os.path.normpath(os.path.join(os.path.dirname(os.path.realpath(__file__)) , ".."))
private_dir = os.path.join(base_dir, "unity", "Assets", "Private")

def current_branch():
    git_dir = os.path.join(base_dir, '.git')
    return subprocess.check_output("git --git-dir=%s rev-parse --abbrev-ref HEAD" % git_dir, shell=True).decode('ascii').strip()


def checkout_branch(directory):
    os.chdir(directory)
    subprocess.check_call("git fetch", shell=True)
    branch = current_branch()
    print("Moving private checkout %s -> %s" % (directory, branch))
    subprocess.check_call("git checkout -B %s" % branch, shell=True)
    subprocess.check_call("git pull origin %s" % branch, shell=True)


if __name__ == "__main__":
    if os.path.isdir(private_dir):
        checkout_branch(private_dir)
    elif os.path.exists(private_dir):
        raise Exception("Private directory %s is not a directory - please remove" % private_dir)
    else:
        subprocess.check_call("git clone %s %s" % (private_repo_url, private_dir), shell=True)
        checkout_branch(private_dir)
