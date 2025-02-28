#!/bin/bash

rm -r /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto
mkdir /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto
cp /opt/nanopb/nanopb-0.4.7-linux-x86/*.h /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto
cp /opt/nanopb/nanopb-0.4.7-linux-x86/*.c /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto

cd /workspaces/Ahsoka.Coprocessor/TempProto
find /workspaces/Ahsoka.Coprocessor/TempProto/*.proto -exec /opt/nanopb/nanopb-0.4.7-linux-x86/generator-bin/nanopb_generator -I /workspaces/Ahsoka.Coprocessor/TempProto/ {} \;
cp *.c /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto
cp *.h /workspaces/Ahsoka.Coprocessor/openview_s/firmware/CM4/Middlewares/Third_Party/proto
cd /workspaces

AHSOKA_COPROCESSOR_PROJECT_PATH=./Ahsoka.Coprocessor/openview_s/firmware
/opt/st/stm32cubeide*/headless-build.sh -data ~/stm32ws -importAll $AHSOKA_COPROCESSOR_PROJECT_PATH -build firmware_CM4/Release

#Clean and Create Output Directory
AHSOKA_BUILD_OUTPUT_ROOT="./BuildOutputs"
AHSOKA_BUILD_OUTPUT="$AHSOKA_BUILD_OUTPUT_ROOT/Firmware/OpenViewLinux_TargetSupport"
mkdir -p $AHSOKA_BUILD_OUTPUT

#Copy CM4 Firmware to TargetSupport
cp $AHSOKA_COPROCESSOR_PROJECT_PATH/CM4/Release/firmware_CM4.elf $AHSOKA_BUILD_OUTPUT
