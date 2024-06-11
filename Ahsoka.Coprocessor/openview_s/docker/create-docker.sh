#!/bin/bash
docker build --build-arg CUBE_IDE_FILE="st-stm32cubeide_1.12.1_16088_20230420_1057_amd64.deb_bundle.sh" -t openpv/openview_cubeide .
