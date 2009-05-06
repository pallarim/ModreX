#!/bin/bash

if [ -z ${1} ] 
then
    echo "missing OpenSim path"
    exit
fi

rm OpenSim
ln -s ${1} OpenSim
