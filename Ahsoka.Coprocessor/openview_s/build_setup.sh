#!/bin/bash

mkdir /workspaces/Ahsoka.Coprocessor
mkdir /workspaces/Ahsoka.Proto
mkdir /workspaces/BuildOutputs

rsync -a /source/Ahsoka.Coprocessor/ /workspaces/Ahsoka.Coprocessor/
rsync -a /source/Ahsoka.Shared/ /workspaces/Ahsoka.Proto/

cp /source/Ahsoka.Extensions.CAN/Proto/CanConfiguration.proto /workspaces/Ahsoka.Proto/
cp /source/Ahsoka.Extensions.CAN/Proto/CanService.proto /workspaces/Ahsoka.Proto/

cd /workspaces
chmod 777 -R .

./Ahsoka.Coprocessor/openview_s/build_firmware.sh

mkdir -p /source/BuildOutputs/Firmware/OpenViewLinux_TargetSupport/
cp ./BuildOutputs/Firmware/OpenViewLinux_TargetSupport/firmware_CM4.elf /source/Ahsoka.Extensions.Can/Resources/OpenViewLinux_TargetSupport_firmware_CM4.elf
