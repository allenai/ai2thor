#!/usr/bin/env python

"""
Script that maintains the Private directory checkout - intended to be run 
immediately after switching branches in the parent ai2thor project
"""
import os
import subprocess

private_repo_url = "https://github.com/allenai/ai2thor-private"
base_dir = os.path.normpath(
    os.path.join(os.path.dirname(os.path.realpath(__file__)), "..")
)
private_dir = os.path.join(base_dir, "unity", "Assets", "Private")


def current_branch():
    git_dir = os.path.join(base_dir, ".git")
    return (
        subprocess.check_output(
            f"git --git-dir={git_dir} rev-parse --abbrev-ref HEAD", shell=True
        )
        .decode("ascii")
        .strip()
    )


def checkout_branch(remote="origin"):
    if not os.path.isdir(private_dir):
        subprocess.check_call(f"git clone {private_repo_url} {private_dir}", shell=True)

    cwd = os.getcwd()
    os.chdir(private_dir)
    branch = current_branch()
    try:
        print(f"Trying to checkout {private_dir} -> {branch}")
        subprocess.check_call(f"git fetch {remote} {branch}", shell=True)
        subprocess.check_call(f"git checkout {branch}", shell=True)
        subprocess.check_call(f"git pull {remote} {branch}", shell=True)
    except subprocess.CalledProcessError as e:
        print(f"No branch exists for private: {branch} - remaining on main")
        subprocess.check_call(f"git fetch {remote} main", shell=True)
        subprocess.check_call(f"git checkout main", shell=True)
        subprocess.check_call(f"git pull {remote} main", shell=True)

    os.chdir(cwd)


if __name__ == "__main__":
    if not os.path.isdir(private_dir) and os.path.exists(private_dir):
        raise Exception(
            f"Private directory {private_dir} is not a directory - please remove"
        )
    else:
        checkout_branch()
