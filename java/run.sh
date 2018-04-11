#!/bin/bash

if [ "$1" == "" ]; then
    echo "Usage: run.sh qualified-class-name [args]"
    exit 1
fi

className=$1
shift

set -e

mvn -q package dependency:copy-dependencies

CLASSPATH=""
for jar in $(ls target/dependency/*.jar target/java-opentracing-tutorial-*.jar); do
  CLASSPATH=$CLASSPATH:$jar
done

java -cp $CLASSPATH $className $*
