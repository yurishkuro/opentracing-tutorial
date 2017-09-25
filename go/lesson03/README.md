# Lesson 3 - Tracing RPC Requests

## Walkthrough

### Hello-World Microservice App

To save you some typing, we are going to start this lesson with a partial solution
available in the [exercise](./exercise) package. We are still working with the same
Hello World application, except that the `formatString` and `printHello` functions
are now rewritten as RPC calls to two downstream services, `formatter` and `publisher`.
The package is organized as follows:

  * `client/hello.go` is the original `hello.go` from Lesson 2 modified to make HTTP calls
  * `formatter/formatter.go` is an HTTP server that responds to a request like
    `GET 'http://localhost:8081/format?helloTo=Bryan'` and returns `"Hello, Bryan!"` string
  * `publisher/publisher.go` is another HTTP server that responds to requests like
     `GET 'http://localhost:8082/publish?helloStr=hi%20there'` and prints "hi there" string to stdout.

To test it out, run the formatter and publisher services in separate terminals

```
$ go run go/lesson03/exercise/formatter/formatter.go
$ go run go/lesson03/exercise/publisher/publisher.go
```

Execute an HTTP request against the formatter:

```
$ curl 'http://localhost:8081/format?helloTo=Bryan'
Hello, Bryan!%
```

Execute and HTTP request against the publisher:

```
$ curl 'http://localhost:8082/publish?helloStr=hi%20there'
```

Note that there will be no output from `curl`, but the publisher stdout will print `"hi there"`.

Finally, if we run the client app as we did in the previous lessons:

```
$ go run go/lesson03/solution/client/hello.go Bryan
2017/09/24 21:43:33 Initializing logging reporter
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:5d10cdd1a9cf004a:7af6719d92c3df6d:1
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:538a7bfd34893922:7af6719d92c3df6d:1
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:7af6719d92c3df6d:0:1
```

We will see the publisher printing the line `"Hello, Bryan!"`.

### Inter-Process Context Propagation

Since the only change we made in the `hello.go` app was to replace two operations with HTTP calls,
the tracing story remains the same - we get a trace with three spans, all from `hello-world` service.
But now we have two more microservices participating in the transaction and we want to se them
in the trace as well. In order to continue the trace over the process boundaries and RPC calls,
we need a way to propagate the span context over the wire. The OpenTracing API provides two functions
in the Tracer interface to do that, `Inject(spanContext, format, carrier)` and `Extract(format, carrier)`.

The `format` parameter refers to one of the three standard encodings the OpenTracing API defines:
  * TextMap where span context is represented as a collection of string key-value pairs,
  * Binary where span context is represented as an opaque byte array,
  * HTTPHeaders, which is similar to TextMap except that the keys must be safe to be used as HTTP headers.

The `carrier` is an abstraction over the underlying RPC framework. For example, a carrier for TextMap
format is an interface that allows the tracer to write key-value pairs via `Set(key, value)` function,
while a carrier for Binary format is simply an `io.Writer`.

The tracing instrumentation uses `Inject` and `Extract` to pass the span context through the RPC calls.

### Instrumenting the Client

In the `formatString` function we already create a child span. In order to pass its context over the HTTP
request we need to call `Inject` on the tracer:

```go
ext.HTTPUrl.Set(span, url)
ext.HTTPMethod.Set(span, "GET")
span.Tracer().Inject(
    span.Context(),
    opentracing.HTTPHeaders,
    opentracing.HTTPHeadersCarrier(req.Header),
)
```

In this case the `carrier` is HTTP request headers object, which we adapt to the carrier API
by wrapping in `opentracing.HTTPHeadersCarrier()`. Notice that we also add a couple additional
tags to the span with some metadata about the HTTP request, as recommended by the OpenTracing
[Semantic Conventions][semantic-conventions]. There are other tags we could add.

We need to add similar code to the `printHello` function.

### Instrumenting the Servers

Our servers are currently not instrumented for tracing. We need to do the following:

#### Add some imports

```go
import (
	opentracing "github.com/opentracing/opentracing-go"
	"github.com/opentracing/opentracing-go/ext"
	otlog "github.com/opentracing/opentracing-go/log"
	"github.com/yurishkuro/opentracing-tutorial/go/lib/jaeger"
)
```

#### Create an instance of a Tracer, similar to how we did it in `hello.go`

```go
tracer, closer := jaeger.Init("formatter")
defer closer.Close()
```

#### Extract the span context from the incoming request using `tracer.Extract`

```go
spanCtx, _ := tracer.Extract(opentracing.HTTPHeaders, opentracing.HTTPHeadersCarrier(r.Header))
```

#### Start a new child span representing the work of the server

```go
span := tracer.StartSpan("format", ext.RPCServerOption(spanCtx))
defer span.Finish()
```

#### Optionally, add tags / logs to that span

```go
span.LogFields(
    otlog.String("event", "string-format"),
    otlog.String("value", helloStr),
)
```

```
# client
$ go run go/lesson03/solution/client/hello.go Bryan
2017/09/24 16:36:06 Initializing logging reporter
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:3535cabe610946bb:731020308bd6d05d:1
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:4ef2c9b5523bca3b:731020308bd6d05d:1
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:731020308bd6d05d:0:1

# formatter
$ go run go/lesson03/solution/formatter/formatter.go
2017/09/24 16:35:56 Initializing logging reporter
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:48394b5372417ee4:3535cabe610946bb:1

# publisher
$ go run go/lesson03/solution/publisher/publisher.go
2017/09/24 16:35:59 Initializing logging reporter
Hello, Bryan!
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:37908db2de452ea2:4ef2c9b5523bca3b:1
```


## Conclusion

The complete program can be found in the [solution](./solution) package. 

Next lesson: [Baggage](../lesson04).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md