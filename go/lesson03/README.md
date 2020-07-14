# Lesson 3 - Tracing RPC Requests

## Objectives

Learn how to:

* Trace a transaction across more than one microservice
* Pass the context between processes using `Inject` and `Extract`
* Apply OpenTracing-recommended tags

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
     `GET 'http://localhost:8082/publish?helloStr=hi%20there'` and prints `"hi there"` string to stdout.

To test it out, run the formatter and publisher services in separate terminals

```
$ go run ./lesson03/exercise/formatter/formatter.go
$ go run ./lesson03/exercise/publisher/publisher.go
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

Note that there will be no output from `curl`, but the publisher stdout will show `"hi there"`.

Finally, if we run the client app as we did in the previous lessons:

```
$ go run ./lesson03/exercise/client/hello.go Bryan
2017/09/24 21:43:33 Initializing logging reporter
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:5d10cdd1a9cf004a:7af6719d92c3df6d:1
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:538a7bfd34893922:7af6719d92c3df6d:1
2017/09/24 21:43:33 Reporting span 7af6719d92c3df6d:7af6719d92c3df6d:0:1
```

We will see the publisher printing the line `"Hello, Bryan!"`.

### Inter-Process Context Propagation

Since the only change we made in the `hello.go` app was to replace two operations with HTTP calls,
the tracing story remains the same - we get a trace with three spans, all from `hello-world` service.
But now we have two more microservices participating in the transaction and we want to see them
in the trace as well. In order to continue the trace over the process boundaries and RPC calls,
we need a way to propagate the span context over the wire. The OpenTracing API provides two functions
in the Tracer interface to do that, `Inject(spanContext, format, carrier)` and `Extract(format, carrier)`.

The `format` parameter refers to one of the three standard encodings the OpenTracing API defines:
  * TextMap where span context is encoded as a collection of string key-value pairs,
  * Binary where span context is encoded as an opaque byte array,
  * HTTPHeaders, which is similar to TextMap except that the keys must be safe to be used as HTTP headers.

The `carrier` is an abstraction over the underlying RPC framework. For example, a carrier for TextMap
format is an interface that allows the tracer to write key-value pairs via `Set(key, value)` function,
while a carrier for Binary format is simply an `io.Writer`.

The tracing instrumentation uses `Inject` and `Extract` to pass the span context through the RPC calls.

### Instrumenting the Client

In the `formatString` function we already create a child span. In order to pass its context over the HTTP
request we need to do the following:

#### Add an import

```go
import (
    "github.com/opentracing/opentracing-go/ext"
)
```

#### Call `Inject` on the tracer

```go
ext.SpanKindRPCClient.Set(span)
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
tags to the span with some metadata about the HTTP request, and we mark the span with a
`span.kind=client` tag, as recommended by the OpenTracing
[Semantic Conventions][semantic-conventions]. There are other tags we could add.

We need to add similar code to the `printHello` function.

#### Handling Errors

Since we turned our single-binary program into a distributed application that makes remote calls, we need to handle errors that may occur during communications. It is a good practice to tag the span with the tag `error=true` if the operation represented by the span failed. So, let's go ahead and update the `format_string` and `print_hello` function with below code snippet:

```go
resp, err := xhttp.Do(req)
if err != nil {
  ext.Error.Set(span, true)
  span.LogFields(
   log.String("event", "error"),
   log.String("value", err.Error()),
  )
  panic(err.Error())
}
```

If either of the Publisher or Formatter are down, our client app will report the error to Jaeger. Jaeger will highlight all such errors in the UI corresponding to the failed span.

### Instrumenting the Servers

Our servers are currently not instrumented for tracing. We need to do the following:

#### Add some imports

```go
import (
    opentracing "github.com/opentracing/opentracing-go"
    "github.com/opentracing/opentracing-go/ext"
    otlog "github.com/opentracing/opentracing-go/log"
    "github.com/yurishkuro/opentracing-tutorial/go/lib/tracing"
)
```

#### Create an instance of a Tracer, similar to how we did it in `hello.go`

```go
tracer, closer := tracing.Init("formatter")
defer closer.Close()
```

#### Extract the span context from the incoming request using `tracer.Extract`

```go
spanCtx, _ := tracer.Extract(opentracing.HTTPHeaders, opentracing.HTTPHeadersCarrier(r.Header))
```

#### Start a new child span representing the work of the server

We use a special option `RPCServerOption` that creates a `ChildOf` reference to the passed `spanCtx`
as well as sets a `span.kind=server` tag on the new span.

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

### Take It For a Spin

As before, first run the `formatter` and `publisher` apps in separate terminals.
Then run the `client/hello.go`. You should see the outputs like this:

```
# client
$ go run ./lesson03/exercise/client/hello.go Bryan
2017/09/24 16:36:06 Initializing logging reporter
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:3535cabe610946bb:731020308bd6d05d:1
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:4ef2c9b5523bca3b:731020308bd6d05d:1
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:731020308bd6d05d:0:1

# formatter
$ go run ./lesson03/exercise/formatter/formatter.go
2017/09/24 16:35:56 Initializing logging reporter
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:48394b5372417ee4:3535cabe610946bb:1

# publisher
$ go run ./lesson03/exercise/publisher/publisher.go
2017/09/24 16:35:59 Initializing logging reporter
Hello, Bryan!
2017/09/24 16:36:06 Reporting span 731020308bd6d05d:37908db2de452ea2:4ef2c9b5523bca3b:1
```

Note how all recorded spans show the same trace ID `731020308bd6d05d`. This is a sign
of correct instrumentation. It is also a very useful debugging approach when something
is wrong with tracing. A typical error is to miss the context propagation somwehere,
either in-process or inter-process, which results in different trace IDs and broken
traces.

If we open this trace in the UI, we should see all five spans.

![Trace](trace.png)

## Conclusion

The complete program can be found in the [solution](./solution) package.

Next lesson: [Baggage](../lesson04).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
