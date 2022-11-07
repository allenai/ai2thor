FROM unityci/editor:ubuntu-2020.3.25f1-base-1.0

# RUN wget https://developer.download.nvidia.com/compute/cuda/11.8.0/local_installers/cuda_11.8.0_455.32.00_linux.run -P /root/
# RUN sh /root/NVIDIA-Linux-x86_64-455.32.00.run -s --no-kernel-module
RUN apt-get -qq update && apt-get -qqy upgrade
RUN apt-get -qqy install xserver-xorg-core xserver-xorg-video-dummy libxcursor1 x11vnc unzip pciutils software-properties-common kmod gcc make linux-headers-generic wget git
COPY start.sh /root/start.sh

RUN git clone https://github.com/allenai/ai2thor /root/ai2thor && \
    cd /root/ai2thor && \
    git checkout nanna-explode

#RUN wget https://repo.anaconda.com/archive/Anaconda3-2021.05-Linux-x86_64.sh
#RUN bash ./Anaconda3-2021.05-Linux-x86_64.sh -b -p /root/anaconda

#RUN ./anaconda3/bin/conda create -n thor-build python=3.9
ENV CONDA_DIR /root/anaconda3
RUN wget --quiet https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh -O ~/miniconda.sh && \
     /bin/bash ~/miniconda.sh -b -p /root/anaconda3
# Put conda in path so we can use conda activate
ENV PATH=$CONDA_DIR/bin:$PATH
RUN . /root/.bashrc && \
    /root/anaconda3/bin/conda init bash && \
    /root/anaconda3/bin/conda create -n thor-build python=3.9 anaconda
#RUN conda create -n thor-build python=3.9
#SHELL ["conda", "run", "-n", "thor-build", "/bin/bash", "-c"]
WORKDIR /root/ai2thor
ENV PIP_ROOT_USER_ACTION=ignore
RUN . /root/.bashrc && \
    conda init bash && \
    conda activate thor-build && \
    python --version && \
    pip install --upgrade pip && \
    pip install --upgrade setuptools && \
    pip install --upgrade distlib && \
    pip install invoke boto3 setuptools && \
    python setup.py install
# RUN conda activate thor-build
RUN python --version
# RUN add-apt-repository ppa:deadsnakes/ppa && apt-get -qqy install python3.9


#RUN update-alternatives --install /usr/bin/python python /usr/bin/python3.9 10
#RUN update-alternatives --install /usr/bin/pip pip /usr/bin/pip3 10
#RUN apt-get -qqy install python3.9-distutils python3-pip
#RUN python --version
#RUN pip3 install --upgrade setuptools && \
#	pip3 install --upgrade pip && \
#	pip3 install --upgrade distlib
#RUN pip3 install invoke boto3 setuptools
#RUN python --version
#RUN python setup.py install