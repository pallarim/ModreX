#!/bin/bash

if [ -z ${1} ] 
then
    echo "missing OpenSim path"
    exit
fi

ln -fs ${1} OpenSim
