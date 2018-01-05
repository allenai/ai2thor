FROM ubuntu:xenial
ARG AI2THOR_VERSION
RUN apt-get update && apt-get -y upgrade
RUN apt-get -y install xserver-xorg-core xserver-xorg-video-dummy libxcursor1 x11vnc unzip pciutils software-properties-common kmod gcc make linux-headers-$(uname -r) wget
COPY unity/builds/thor-$AI2THOR_VERSION-Linux64  /root/thor-$AI2THOR_VERSION-Linux64/thor-$AI2THOR_VERSION-Linux64
COPY unity/builds/thor-$AI2THOR_VERSION-Linux64_Data  /root/thor-$AI2THOR_VERSION-Linux64/thor-$AI2THOR_VERSION-Linux64_Data
COPY start.sh /root/start.sh
