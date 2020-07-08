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


def checkout_branch():
    if not os.path.isdir(private_dir):
        subprocess.check_call("git clone %s %s" % (private_repo_url, private_dir), shell=True)

    cwd = os.getcwd()
    os.chdir(private_dir)
    branch = current_branch()
    try:
        print("Trying to checkout checkout %s -> %s" % (private_dir, branch))
        subprocess.check_call("git fetch origin %s" % branch, shell=True)
        subprocess.check_call("git checkout %s" % branch, shell=True)
        subprocess.check_call("git pull origin %s" % branch, shell=True)
    except subprocess.CalledProcessError as e:
        print("No branch exists for private: %s - remaining on master" % branch )
        subprocess.check_call("git fetch origin master", shell=True)
        subprocess.check_call("git checkout master", shell=True)
        subprocess.check_call("git pull origin master", shell=True)

    os.chdir(cwd)


if __name__ == "__main__":
    if not os.path.isdir(private_dir) and os.path.exists(private_dir):
        raise Exception("Private directory %s is not a directory - please remove" % private_dir)
    else:
        checkout_branch()
