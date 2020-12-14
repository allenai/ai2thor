#!/bin/bash

if [[ -z "$TRAVIS_TAG" ]]; then
    echo "TRAVIS_TAG must be set for build/deploy"
    exit 1
fi

# we need to have __version__ for pip3 install to succeed
# it also will get set in build-pip with additional sanity checks
echo "__version__ = '$TRAVIS_TAG'" >> ai2thor/_version.py

pip3 install -e .
# pinning keyring to 21.4.0 due to version conflict created see(https://travis-ci.community/t/cant-deploy-to-pypi-anymore-pkg-resources-contextualversionconflict-importlib-metadata-0-18/10494/4)
pip3 install twine wheel invoke keyring==21.4.0

invoke build-pip $TRAVIS_TAG && invoke deploy-pip

