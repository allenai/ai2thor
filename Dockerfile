FROM ubuntu:xenial
RUN apt-get -qq update && apt-get -qqy upgrade
RUN apt-get -qqy install xserver-xorg-core xserver-xorg-video-dummy libxcursor1 x11vnc unzip pciutils software-properties-common kmod gcc make linux-headers-generic wget
COPY start.sh /root/start.sh
