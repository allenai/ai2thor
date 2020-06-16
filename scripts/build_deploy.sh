#!/bin/bash
pip3 install -e .
pip3 install twine wheel invoke

if [[ $(git rev-parse --is-shallow-repository) == 'true' ]]; then
    git fetch --unshallow
    git config remote.origin.fetch "+refs/heads/*:refs/remotes/origin/*"
    git fetch origin
fi;

invoke build-pip
invoke deploy-pip
