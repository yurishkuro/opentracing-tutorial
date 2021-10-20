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

# Between Java 8 and 11 (not inclusive), we needed to add java.xml.bind to the list of modules to load
JAVA_VERSION=$(java -version 2>&1 | awk -F '"' '/version/ {print $2}')
ADD_MODULES=""
if [[ "$version" > "1.8" ]] && [[ "$version" < "11" ]]; then
  ADD_MODULES="--add-modules=java.xml.bind"
fi

java $ADD_MODULES -cp $CLASSPATH $className $*
