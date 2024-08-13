#!/usr/bin/env python
import os
import subprocess
import sys
from typing import Optional

private_dir = ""
private_repo_url = ""


class Repo:
    def __init__(
        self,
        url: str,
        target_dir: str,
        delete_before_checkout: bool,
        commit_id=None,
        branch=None,
    ) -> None:
        self.url = url
        self.target_dir = target_dir
        self.base_dir = os.path.normpath(
            os.path.join(os.path.dirname(os.path.realpath(__file__)), "..")
        )
        self.commit_id = commit_id
        self.branch = branch
        self.delete_before_checkout = delete_before_checkout

    def current_branch(self):
        git_dir = os.path.join(self.base_dir, ".git")
        return (
            subprocess.check_output(
                f"git --git-dir={git_dir} rev-parse --abbrev-ref HEAD", shell=True
            )
            .decode("ascii")
            .strip()
        )

    def checkout_branch(self, remote="origin"):
        if not os.path.isdir(self.target_dir):
            subprocess.check_call(f"git clone {self.url} {self.target_dir}", shell=True)

        cwd = os.getcwd()
        os.chdir(self.target_dir)
        branch = self.current_branch() if self.branch is None else self.branch
        try:
            print(f"Trying to checkout {self.target_dir} -> {branch}")
            subprocess.check_call(f"git fetch {remote}", shell=True)
            if self.commit_id is None:
                subprocess.check_call(f"git checkout {branch}", shell=True)
                subprocess.check_call(f"git pull {remote} {branch}", shell=True)
            else:
                subprocess.check_call(f"git checkout {self.commit_id}", shell=True)
        except subprocess.CalledProcessError as e:
            print(f"No branch exists for private: {branch} - remaining on main")
            subprocess.check_call(f"git fetch {remote} main", shell=True)
            if self.commit_id is None:
                subprocess.check_call(f"git checkout main", shell=True)
                subprocess.check_call(f"git pull {remote} main", shell=True)
            else:
                subprocess.check_call(f"git checkout {self.commit_id}", shell=True)

        os.chdir(cwd)


"""
Script that maintains the Private directory checkout - intended to be run 
immediately after switching branches in the parent ai2thor project
"""
if __name__ == "__main__":
    if len(sys.argv[1]) < 3:
        raise Exception(
            f"Missing args, scipt should be run `update_private.py <private_repo_url> <private_dir>`"
        )
    private_dir = sys.argv[1]
    private_repo_url = sys.argv[2]
    if not os.path.isdir(private_dir) and os.path.exists(private_dir):
        raise Exception(f"Private directory {private_dir} is not a directory - please remove")
    else:
        repo = Repo(url=private_repo_url, target_dir=private_dir, delete_before_checkout=True)
        repo.checkout_branch()
