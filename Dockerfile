FROM ubuntu:xenial
RUN apt-get update && apt-get -y upgrade
RUN apt-get -y install xserver-xorg-core xserver-xorg-video-dummy libxcursor1 x11vnc unzip pciutils software-properties-common kmod gcc make linux-headers-generic wget
COPY start.sh /root/start.sh
