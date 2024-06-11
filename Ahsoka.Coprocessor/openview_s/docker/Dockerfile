FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive

SHELL ["/bin/bash", "-c"]

# Install the Toolchain
#ENV CUBE_IDE_FILE=st-stm32cubeide_1.11.2_14494_20230119_0724.unsigned_amd64.deb_bundle.sh
ARG CUBE_IDE_FILE
WORKDIR /root
COPY $CUBE_IDE_FILE .
RUN \
    chmod +x $CUBE_IDE_FILE && \
    ./$CUBE_IDE_FILE --noexec --target extract && \
    rm -f $CUBE_IDE_FILE

WORKDIR /root/extract
ENV LICENSE_ALREADY_ACCEPTED=1

RUN \
    apt-get update --fix-missing && \
    apt-get upgrade -y && \
    apt-get install -y --no-install-recommends \
        git \
		rsync \
        ./st-stlink-server-*-linux-amd64.deb \
        ./st-stlink-udev-rules-*-linux-all.deb \
        ./st-stm32cubeide-*_amd64.deb && \
    apt-get clean -y && \
    rm -rf /var/lib/apt/lists/* && \
    rm -rf /root/extract;

RUN apt-get update && \
	apt-get install -y protobuf-compiler wget python3 python3-protobuf && \
	rm -rf /var/lib/apt/lists/*

RUN cd /opt && mkdir nanopb && cd nanopb && \
    wget https://jpa.kapsi.fi/nanopb/download/nanopb-0.4.7-linux-x86.tar.gz && \
    tar xzf nanopb-0.4.7-linux-x86.tar.gz && \
    rm nanopb-0.4.7-linux-x86.tar.gz

ADD entrypoint.sh /home/user/entrypoint.sh

WORKDIR /workspaces

ENTRYPOINT bash -l
