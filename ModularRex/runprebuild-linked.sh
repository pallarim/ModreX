#!/bin/bash
sed 's/\.\.\/\.\.\/bin/OpenSim\/bin/' prebuild.xml > linked-prebuild.xml
mono ./OpenSim/bin/Prebuild.exe /file linked-prebuild.xml /target nant
mono ./OpenSim/bin/Prebuild.exe /file linked-prebuild.xml /target monodev
cp ModularRex.build default.build
