#!/bin/bash

SED=/bin/sed

function escape_path 
{
    echo $(echo $1 | ${SED} 's|/|\\/|g' | ${SED} 's|\.|\\.|g')
}

FILENAME=$1
PATHNAME=$2
OLDPATH=$(escape_path $3)
NEWPATH=$(escape_path $4)

rebasecmd="s|<${PATHNAME}>\(.*\)${OLDPATH}\(.*\)</${PATHNAME}>|<${PATHNAME}>\1${NEWPATH}\2</${PATHNAME}>|g"

${SED} -i ${rebasecmd} ${FILENAME}
