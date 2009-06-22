#!/bin/bash
#sed 's/\.\.\/\.\.\/bin/OpenSim\/bin/' prebuild.xml > linked-prebuild.xml
cp prebuild.xml rebased-prebuild.xml
./rebase-prebuild-path.sh rebased-prebuild.xml OutputPath "../../bin" "OpenSim/bin"
./rebase-prebuild-path.sh rebased-prebuild.xml ReferencePath "../../bin" "OpenSim/bin"
mono ./OpenSim/bin/Prebuild.exe /file rebased-prebuild.xml /target nant
mono ./OpenSim/bin/Prebuild.exe /file rebased-prebuild.xml /target monodev
cp ModularRex.build default.build
