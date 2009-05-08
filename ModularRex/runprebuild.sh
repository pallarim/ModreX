#!/bin/bash
mono ../../bin/Prebuild.exe /target nant
mono ../../bin/Prebuild.exe /target monodev
cp ModularRex.build default.build
