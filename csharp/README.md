# OpenTracing Tutorial - Java

## Installing

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

This repository uses Maven to manage dependencies. To install all dependencies, run:

```
cd opentracing-tutorial/java
mvn package
```

All subsequent commands in the tutorials should be executed relative to this `java` directory.

## Lessons

* [Lesson 01 - Hello World](./src/main/java/lesson01)
  * Instantiate a Tracer
  * Create a simple trace
  * Annotate the trace
* [Lesson 02 - Context and Tracing Functions](./src/main/java/lesson02)
  * Trace individual functions
  * Combine multiple spans into a single trace
  * Propagate the in-process context
* [Lesson 03 - Tracing RPC Requests](./src/main/java/lesson03)
  * Trace a transaction across more than one microservice
  * Pass the context between processes using `Inject` and `Extract`
  * Apply OpenTracing-recommended tags
* [Lesson 04 - Baggage](./src/main/java/lesson04)
  * Understand distributed context propagation
  * Use baggage to pass data through the call graph
* [Extra Credit](./src/main/java/extracredit)
  * Use existing open source instrumentation
