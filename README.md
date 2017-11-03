# RobotAIPlatform

Assets
------
[HQ Residential House](https://www.assetstore.unity3d.com/en/#!/content/48976)

[Modern Living Room](https://www.assetstore.unity3d.com/en/#!/content/41772)

Install Unity <link to unity>
Download from S3/HQ Res/Modernliving
Build
start_server # rename to start

# Linux Development Setup
Launch new EC2 instance with tag Context: vision
```bash
# copy boto credentials to $HOME/.boto
sudo apt-get install python-virtualenv python3 python3-dev libffi-dev
cd robosims;
virtualenv venv --system-site-packages --python=/usr/bin/python3
source venv/bin/activate
pip install pip --upgrade
pip install -r requirements.txt
```
The latest NVIDIA Graphics driver should be installed using NVIDIA install package http://www.nvidia.com/Download/index.aspx?lang=en-us or the latest version of CUDA.

# MacOS Development Setup

```bash
# copy boto credentials to $HOME/.boto

brew install python3 libffi
pip install virtualenv

git clone https://github.com/allenai/robosims
cd robosims
virtualenv venv --system-site-packages --python=python3
source venv/bin/activate
pip install pip --upgrade
pip install -r requirements.txt

# If pddlpy emits warnings about antlr disagreement 4.6!=4.53
# this can be resolved by doing the following:
git clone https://github.com/hfoffani/pddl-lib
# edit Makefile to point to correct antlr directory and version
# ANTLRDIR=/usr/local/Cellar/antlr/4.6
# ANTLRLIB=$(ANTLRDIR)/antlr-4.6-complete.jar
make pyparsers
python setup.py install

```
