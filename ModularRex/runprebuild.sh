#!/bin/bash
mono ./OpenSim/bin/Prebuild.exe /target nant
mono ./OpenSim/bin/Prebuild.exe /target monodev
cp ModularRex.build default.build
