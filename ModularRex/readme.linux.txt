ModreX relies on OpenSim, and must install its DLLs into the OpenSim source tree. There are two ways to let ModreX know where OpenSim is:

1. Place modrex within OpenSim source tree.
  ex: cd opensim/; svn co http://forge.opensimulator.org/svn/modrex/trunk modrex; cd modrex/ModularRex; ./runprebuild.sh

2. Use the link-opensim.sh script to tell ModreX where OpenSim is using a symbolic link.
  ex: svn co http://forge.opensimulator.org/svn/modrex/trunk/ modrex; cd modrex/ModularRex; link-opensim.sh $OPENSIM_DIR; ./runprebuild-linked.sh
