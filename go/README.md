# OpenTracing Tutorial - Go

## Installing

This repository uses [glide](https://github.com/Masterminds/glide) to manage dependencies.

All dependencies can be installed by running `make install`.

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

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
  * Apply special `span.kind` and standard HTTP tags
* [Lesson 04 - Baggage](lesson04)
