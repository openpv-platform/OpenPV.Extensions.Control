#!/bin/bash
docker run -p 5000:22 -it -v "$(pwd)"/../firmware:/firmware --name OpenViewCubeIDE openpv/openview_cubeide
