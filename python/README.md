# OpenTracing Tutorial - Python

## Installing

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

Jaeger Python client currently only supports Python 2.7.

This repository uses [virtualenv](https://pypi.python.org/pypi/virtualenv) and `pip` to manage dependencies.
To install all dependencies, run:

```
cd opentracing-tutorial/python
virtualenv env
pip install -r requirements.txt
```

All subsequent commands in the tutorials should be executed relative to this `python` directory.

## Lessons

* [Lesson 01 - Hello World](./lesson01)
  * Instantiate a Tracer
  * Create a simple trace
  * Annotate the trace
* [Lesson 02 - Context and Tracing Functions](./lesson02)
  * Trace individual functions
  * Combine multiple spans into a single trace
  * Propagate the in-process context
* [Lesson 03 - Tracing RPC Requests](./lesson03)
  * Trace a transaction across more than one microservice
  * Pass the context between processes using `Inject` and `Extract`
  * Apply OpenTracing-recommended tags
* [Lesson 04 - Baggage](./lesson04)
  * Understand distributed context propagation
  * Use baggage to pass data through the call graph
