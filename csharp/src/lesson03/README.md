# Lesson 3 - Tracing HTTP Requests

## Objectives

Learn how to:

* Trace a transaction across more than one microservice
* Pass the context between processes using `Inject` and `Extract`
* Apply OpenTracing-recommended tags

## Walkthrough

### Hello-World Microservice App

To save you some typing, we are going to start this lesson with a partial solution
available in the [exercise](./exercise) package. We are using the same
Hello World application as base embedded in a REST API project. The `formatString` and `printHello` functions
are now rewritten as REST API calls to the endpoints `format` and `publish`.
The package is organized as follows:

  * `Hello.cs` is a copy from Lesson 2 modified to make HTTP calls.
  * `FormatController.cs` is the controller that responds to requests like
    `GET 'http://localhost:56870/api/format/Bryan'` and returns the string `"Hello, Bryan!"`.
  * `PublishController.cs` is the controller that responds to requests like
     `GET 'http://localhost:56870/api/publish/hi%20there'` and prints `"hi there"` string to stdout,
    returning the string `published`.

To test it out, run the project in Visual Studio and observe the output from the ASP.NET Core Web Server.

Execute an HTTP request against the formatter:

```
$ curl 'http://localhost:56870/api/format/Bryan'
Hello, Bryan!
```

Execute and HTTP request against the publisher:

```
$ curl 'http://localhost:56870/api/publish/hello'
published
```

The publisher stdout will show `"hello"`.

Finally, if access the endpoint initiating the call to our code as we did in the previous lessons
we see the same three spans in the output as before:

```
$ curl 'http://localhost:56870/api/hello/Bryan'
info: Jaeger.Core.Reporters.LoggingReporter[0]
      Reporting span:
 {
        "Context": {
          "TraceId": {
            "High": 6165735122331609763,
            "Low": 5774498728660989581,
            "IsValid": true
          },
          ...
        },
        ...
      }
Hello Bryan!
published
info: Jaeger.Core.Reporters.LoggingReporter[0]
      Reporting span:
 {
        "Context": {
          "TraceId": {
            "High": 6165735122331609763,
            "Low": 5774498728660989581,
            "IsValid": true
          },
          },
          ...
        },
        ...
      }
info: Jaeger.Core.Reporters.LoggingReporter[0]
      Reporting span:
 {
        "Context": {
          "TraceId": {
            "High": 6165735122331609763,
            "Low": 5774498728660989581,
            "IsValid": true
          },
          },
          ...
        },
        ...
      }
```

### Inter-Process Context Propagation

Since the only change we made in the `Hello.java` app was to replace two operations with HTTP calls,
the tracing story remains the same - we get a trace with three spans, all from `hello-world` service.
But now we have two more microservices participating in the transaction and we want to see them
in the trace as well. In order to continue the trace over the process boundaries and RPC calls,
we need a way to propagate the span context over the wire. The OpenTracing API provides two functions
in the Tracer interface to do that, `inject(spanContext, format, carrier)` and `extract(format, carrier)`.

The `format` parameter refers to one of the three standard encodings the OpenTracing API defines:
  * `TEXT_MAP` where span context is encoded as a collection of string key-value pairs,
  * `BINARY` where span context is encoded as an opaque byte array,
  * `HTTP_HEADERS`, which is similar to `TEXT_MAP` except that the keys must be safe to be used as HTTP headers.

The `carrier` is an abstraction over the underlying RPC framework. For example, a carrier for `TEXT_MAP`
format is an interface that allows the tracer to write key-value pairs via `put(key, value)` method,
while a carrier for Binary format is simply a `ByteBuffer`.

The tracing instrumentation uses `inject` and `extract` to pass the span context through the RPC calls.

### Instrumenting the Client

In the `formatString` function we already create a child span. In order to pass its context over the HTTP
request we need to call `tracer.inject` before building the HTTP request:

```java
Tags.SPAN_KIND.set(tracer.activeSpan(), Tags.SPAN_KIND_CLIENT);
Tags.HTTP_METHOD.set(tracer.activeSpan(), "GET");
Tags.HTTP_URL.set(tracer.activeSpan(), url.toString());
tracer.inject(tracer.activeSpan().context(), Builtin.HTTP_HEADERS, new RequestBuilderCarrier(requestBuilder));
```

In this case the `carrier` is HTTP request headers object, which we adapt to the carrier API
by wrapping in `RequestBuilderCarrier` helper class. 

```java
private static class RequestBuilderCarrier implements io.opentracing.propagation.TextMap {
    private final Request.Builder builder;

    RequestBuilderCarrier(Request.Builder builder) {
        this.builder = builder;
    }

    @Override
    public Iterator<Entry<String, String>> iterator() {
        throw new UnsupportedOperationException("carrier is write-only");
    }

    @Override
    public void put(String key, String value) {
        builder.addHeader(key, value);
    }
}
```

Notice that we also add a couple additional tags to the span with some metadata about the HTTP request,
and we mark the span with a `span.kind=client` tag, as recommended by the OpenTracing
[Semantic Conventions][semantic-conventions]. There are other tags we could add.

### Instrumenting the Servers

Our servers are currently not instrumented for tracing. We need to do the following:

#### Add some imports

```java
import io.opentracing.Scope;
import io.opentracing.Tracer;
import lib.Tracing;
```

#### Create an instance of a Tracer, similar to how we did it in `Hello.java`

Add a member variable and a constructor to the Formatter:

```java
private final Tracer tracer;

private Formatter(Tracer tracer) {
    this.tracer = tracer;
}
```

Replace the call to `Formatter.run()` with this:

```java
Tracer tracer = Tracing.init("formatter");
new Formatter(tracer).run(args);
```

#### Extract the span context from the incoming request using `tracer.extract`

First, add a helper function:

```java
public static Scope startServerSpan(Tracer tracer, javax.ws.rs.core.HttpHeaders httpHeaders,
        String operationName) {
    // format the headers for extraction
    MultivaluedMap<String, String> rawHeaders = httpHeaders.getRequestHeaders();
    final HashMap<String, String> headers = new HashMap<String, String>();
    for (String key : rawHeaders.keySet()) {
        headers.put(key, rawHeaders.get(key).get(0));
    }

    Tracer.SpanBuilder spanBuilder;
    try {
        SpanContext parentSpan = tracer.extract(Format.Builtin.HTTP_HEADERS, new TextMapExtractAdapter(headers));
        if (parentSpan == null) {
            spanBuilder = tracer.buildSpan(operationName);
        } else {
            spanBuilder = tracer.buildSpan(operationName).asChildOf(parentSpan);
        }
    } catch (IllegalArgumentException e) {
        spanBuilder = tracer.buildSpan(operationName);
    }
    return spanBuilder.withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_SERVER).startActive(true);
}
```

The logic here is similar to the client side instrumentation, except that we are using `tracer.extract`
and tagging the span as `span.kind=server`. Instead of using a dedicated adapter class to convert
JAXRS `HttpHeaders` type into `io.opentracing.propagation.TextMap`, we are copying the headers to a plain
`HashMap<String, String>` and using a standard adapter `TextMapExtractAdapter`.

Now change the `FormatterResource` handler method to use `startServerSpan`:

```java
@GET
public String format(@QueryParam("helloTo") String helloTo, @Context HttpHeaders httpHeaders) {
    try (Scope scope = Tracing.startServerSpan(tracer, httpHeaders, "format")) {
        String helloStr = String.format("Hello, %s!", helloTo);
        scope.span().log(ImmutableMap.of("event", "string-format", "value", helloStr));
        return helloStr;
    }
}
```

### Take It For a Spin

As before, first run the `formatter` and `publisher` apps in separate terminals.
Then run `lesson03.exercise.Hello`. You should see the outputs like this:

```
# client
$ ./run.sh lesson03.exercise.Hello Bryan
INFO com.uber.jaeger.Configuration - Initialized tracer=Tracer(...)
INFO com.uber.jaeger.reporters.LoggingReporter - Span reported: 5fe2d9de96c3887a:72910f6018b1bd09:5fe2d9de96c3887a:1 - formatString
INFO com.uber.jaeger.reporters.LoggingReporter - Span reported: 5fe2d9de96c3887a:62d73167c129ecd7:5fe2d9de96c3887a:1 - printHello
INFO com.uber.jaeger.reporters.LoggingReporter - Span reported: 5fe2d9de96c3887a:5fe2d9de96c3887a:0:1 - say-hello

# formatter
$ ./run.sh lesson03.exercise.Formatter server
[skip noise]
INFO org.eclipse.jetty.server.Server: Started @3968ms
INFO com.uber.jaeger.reporters.LoggingReporter: Span reported: 5fe2d9de96c3887a:b73ff97ea68a36f8:72910f6018b1bd09:1 - format
127.0.0.1 - - "GET /format?helloTo=Bryan HTTP/1.1" 200 13 "-" "okhttp/3.9.0" 3

# publisher
$ ./run.sh lesson03.exercise.Publisher server
[skip noise]
INFO org.eclipse.jetty.server.Server: Started @3388ms
Hello, Bryan!
INFO com.uber.jaeger.reporters.LoggingReporter: Span reported: 5fe2d9de96c3887a:4a2c39e462cb2a92:62d73167c129ecd7:1 - publish
127.0.0.1 - - "GET /publish?helloStr=Hello,%20Bryan! HTTP/1.1" 200 9 "-" "okhttp/3.9.0" 80
```

Note how all recorded spans show the same trace ID `5fe2d9de96c3887a`. This is a sign
of correct instrumentation. It is also a very useful debugging approach when something
is wrong with tracing. A typical error is to miss the context propagation somwehere,
either in-process or inter-process, which results in different trace IDs and broken
traces.

If we open this trace in the UI, we should see all five spans.

![Trace](../../../../../go/lesson03/trace.png)

## Conclusion

The complete program can be found in the [solution](./solution) package.

Next lesson: [Baggage](../lesson04).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
