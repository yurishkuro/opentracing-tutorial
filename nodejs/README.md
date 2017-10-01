# OpenTracing Tutorial - Node.js

## Installing

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

Install the dependencies:

```
cd opentracing-tutorial/nodejs
npm install
```

The rest of the commands in the Node.js tutorials should be executed relative to this directory.

## Under construction

This tutorial is currently incomplete. You can try following the tutorials in the other languages
and adapt them to Node.js, as all the same features are available in the OpenTracing API for Javascript
(https://github.com/opentracing/opentracing-javascript).  Use the following code to initialize Jaeger tracer:

```javascript
var initJaegerTracer = require('jaeger-client').initTracer;

function initTracer(serviceName) {
    var config = {
        'serviceName': serviceName,
        'sampler': {
            'type': 'const',
            'param': 1
        },
        'reporter': {
            'logSpans': true
        }
      };
      var options = {
        'logger': {
            'info': function logInfo(msg) {
                console.log('INFO ', msg);
            },
            'error': function logError(msg) {
                console.log('ERROR', msg)
            }
        }
      };
      return initJaegerTracer(config, options);
}
```

Note that Lesson 1 has a solution provided, which you can use to bootstrap your work for the other lessons.

<!--
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
* [Extra Credit](./extracredit)
  * Use existing open source instrumentation
-->