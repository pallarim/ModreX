#!/bin/bash

MODREX_DIR="ModularRex"
MODREX_CONFIG_DIR="addon-modules/ModreX/config"
MODREX_SCRIPT_DIR="$MODREX_DIR/ScriptEngines"

OPENSIM_DIR="../../bin"
OPENSIM_SCRIPT_DIR="$OPENSIM_DIR/ScriptEngines"

# deploy modrex.ini
mkdir -p $OPENSIM_DIR/$MODREX_CONFIG_DIR
cp -n $MODREX_DIR/$MODREX_CONFIG_DIR/modrex.ini $OPENSIM_DIR/$MODREX_CONFIG_DIR

# deploy python scripting engine
cp -a -f $MODREX_SCRIPT_DIR/Lib $OPENSIM_SCRIPT_DIR
cp -a -f $MODREX_SCRIPT_DIR/PythonScript $OPENSIM_SCRIPT_DIR
