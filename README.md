# OpenTracing Tutorials

A collection of tutorials for the OpenTracing API (https://opentracing.io).

## Prerequisites

The tutorials are using CNCF Jaeger (https://jaegertracing.io) as the tracing backend.
The full backend with a mock in-memory storage can be run as a single Docker container (one command):

```
docker run -d -p5775:5775/udp -p6831:6831/udp -p6832:6832/udp \
  -p5778:5778 -p16686:16686 -p14268:14268 -p9411:9411 jaegertracing/all-in-one:1.7.0
```

Alternatively, it can be downloaded as a binary called `all-in-one` for different platforms from https://jaegertracing.io/download/.

Once the backend starts, the Jaeger UI will be accessible at http://localhost:16686.

## Tutorials by Language

  * [C# tutorial](./csharp/)
  * [Go tutorial](./go/)
  * [Java tutorial](./java)
  * [Python tutorial](./python)
  * [Node.js tutorial](./nodejs)
