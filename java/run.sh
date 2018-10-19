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

ADD_MODULES=""
if [ "$(java -version 2>&1 | head -1 | grep '\"1\.[78].\+\"')" = "" ]; then
  ADD_MODULES="--add-modules=java.xml.bind"
fi

java $ADD_MODULES -cp $CLASSPATH $className $*
