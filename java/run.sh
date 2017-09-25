#!/bin/bash

if [ "$1" == "" ]; then
    echo "Usage: run.sh qualified-class-name [args]"
    exit 1
fi

className=$1
shift

mvn exec:java -Dexec.mainClass="$className" -Dexec.args="$*" | grep -v -e '\[INFO\]' -e '\[WARNING\]' -e '\[ERROR\]'
