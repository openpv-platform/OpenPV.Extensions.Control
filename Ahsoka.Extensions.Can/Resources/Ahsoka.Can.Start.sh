#!/bin/sh
echo "CAN Start Time" $(date +%T.%3N)

#Start Application
echo start > /sys/class/remoteproc/remoteproc0/state

until [ -e ${InterfacePath} ]; do
    echo "Waiting for CAN App to Start..."
    sleep 1
done

#Start Application
#slattach -p slip -L -m ${InterfacePath} &
#ifconfig sl0 ${IpAddressLocal} pointtopoint ${IpAddressRemote} up
