# OpenTracing Tutorial - Go

## Installing

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

This repository uses [glide](https://github.com/Masterminds/glide) to manage dependencies (installed automatically below).

When you clone the tutorials repository, it should be located in the right place under `$GOPATH`:

```
mkdir -p $GOPATH/src/github.com/yurishkuro/
cd $GOPATH/src/github.com/yurishkuro/
git clone https://github.com/yurishkuro/opentracing-tutorial.git opentracing-tutorial
```

After that, install the dependencies:

```
cd $GOPATH/src/github.com/yurishkuro/opentracing-tutorial/go
make install
```

The rest of the commands in the Go tutorials should be executed relative to this directory.

## Lessons

* [Lesson 01 - Hello World](lesson01)
  * Instantiate a Tracer
  * Create a simple trace
  * Annotate the trace
* [Lesson 02 - Context and Tracing Functions](lesson02)
  * Trace individual functions
  * Combine multiple spans into a single trace
  * Propagate the in-process context
* [Lesson 03 - Tracing RPC Requests](lesson03)
  * Trace a transaction across more than one microservice
  * Pass the context between processes using `Inject` and `Extract`
  * Apply OpenTracing-recommended tags
* [Lesson 04 - Baggage](lesson04)
  * Understand distributed context propagation
  * Use baggage to pass data through the call graph
