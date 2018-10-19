# OpenTracing Tutorials

A collection of tutorials for the OpenTracing API (https://opentracing.io).

## Prerequisites

The tutorials are using CNCF Jaeger (https://jaegertracing.io) as the tracing backend.
For this tutorial, we'll start Jaeger via Docker with the default in-memory storage, exposing only the required ports. We'll also enable "debug" level logging:

```
docker run \
  --rm \
  -p 6831:6831/udp \
  -p 16686:16686 \
  jaegertracing/all-in-one:1.7 \
  --log-level=debug
```

Alternatively, Jaeger can be downloaded as a binary called `all-in-one` for different platforms from https://jaegertracing.io/download/.

Once the backend starts, the Jaeger UI will be accessible at http://localhost:16686.

## Tutorials by Language

  * [C# tutorial](./csharp/)
  * [Go tutorial](./go/)
  * [Java tutorial](./java)
  * [Python tutorial](./python)
  * [Node.js tutorial](./nodejs)
