#!/bin/bash

mkdir /workspaces/Ahsoka.Coprocessor
mkdir /workspaces/BuildOutputs

rsync -a /source/Ahsoka.Coprocessor/ /workspaces/Ahsoka.Coprocessor/

cd /workspaces
chmod 777 -R .

./Ahsoka.Coprocessor/openview_s/build_firmware.sh

mkdir -p /source/BuildOutputs/Firmware/OpenViewLinux_TargetSupport/
cp ./BuildOutputs/Firmware/OpenViewLinux_TargetSupport/firmware_CM4.elf /source/Ahsoka.Extensions.Can/Resources/OpenViewLinux_TargetSupport_firmware_CM4.elf
