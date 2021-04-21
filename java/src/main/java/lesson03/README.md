# Lesson 3 - Tracing RPC Requests

## Objectives

Learn how to:

* Trace a transaction across more than one microservice
* Pass the context between processes using `Inject` and `Extract`
* Apply OpenTracing-recommended tags

## Walkthrough

### Hello-World Microservice App

We'll start this lesson with some seed files in our `exercise` directory. This is mainly what we've accomplished in the
previous lessons, plus a small refactoring to avoid duplicating code. We've also changed the `formatString` and 
`printHello` methods to make RPC calls to two downstream services, `formatter` and `publisher`. The package is organized
as follows:

  * `Hello.java` is based on the code from the lesson 2, modified to make HTTP calls
  * `Formatter.java` is a Dropwizard-based HTTP server that responds to a request like
    `GET 'http://localhost:8081/format?helloTo=Bryan'` and returns `"Hello, Bryan!"` string
  * `Publisher.java` is another HTTP server that responds to requests like
     `GET 'http://localhost:8082/publish?helloStr=hi%20there'` and prints `"hi there"` string to stdout.

To test it out, run the formatter and publisher services in separate terminals

```
$ ./run.sh lesson03.exercise.Formatter server
$ ./run.sh lesson03.exercise.Publisher server
```

Execute an HTTP request against the formatter:

```
$ curl 'http://localhost:8081/format?helloTo=Bryan'
Hello, Bryan!
```

Execute and HTTP request against the publisher:

```
$ curl 'http://localhost:8082/publish?helloStr=hi%20there'
published
```

The publisher stdout will show `"hi there"`.

Finally, if we run the client app as we did in the previous lessons:

```
$ ./run.sh lesson03.exercise.Hello Bryan
INFO io.jaegertracing.Configuration - Initialized tracer=JaegerTracer(version=Java-0.32.0, serviceName=hello-world, ...)
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 3375c5bb090033f5:3b92b00e99b6d74c:3375c5bb090033f5:1 - formatString
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 3375c5bb090033f5:45021dd8d1095091:3375c5bb090033f5:1 - printHello
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 3375c5bb090033f5:3375c5bb090033f5:0:1 - say-hello
```

We will see the `publisher` printing the line `"Hello, Bryan!"`.

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

In the `Hello#formatString()` function we already create a child span. In order to pass its context over the HTTP
request we need to call `tracer.inject` before building the HTTP request in `Hello#getHttp()`

```java
import io.opentracing.propagation.Format;
import io.opentracing.tag.Tags;

Span activeSpan = tracer.activeSpan();
Tags.SPAN_KIND.set(activeSpan, Tags.SPAN_KIND_CLIENT);
Tags.HTTP_METHOD.set(activeSpan, "GET");
Tags.HTTP_URL.set(activeSpan, url.toString());
tracer.inject(activeSpan.context(), Format.Builtin.HTTP_HEADERS, new RequestBuilderCarrier(requestBuilder));
```

In this case the `carrier` is HTTP request headers object, which we adapt to the carrier API
by wrapping in `RequestBuilderCarrier` helper class. 

```java
import java.util.Iterator;
import java.util.Map;

import okhttp3.Request;

public class RequestBuilderCarrier implements io.opentracing.propagation.TextMap {
    private final Request.Builder builder;

    RequestBuilderCarrier(Request.Builder builder) {
        this.builder = builder;
    }

    @Override
    public Iterator<Map.Entry<String, String>> iterator() {
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

#### Handling Errors
Since we turned our single-binary program into a distributed application that makes remote calls, we need to handle errors that may occur during communications. It is a good practice to tag the span with the tag `error=true` if the operation represented by the span failed. So, let's go ahead and update the `catch` section of `get_http` function with below code snippet:

```java
catch (IOException e) {
    Tags.ERROR.set(tracer.activeSpan(), true);
    tracer.activeSpan().log(ImmutableMap.of(Fields.EVENT, "error", Fields.ERROR_OBJECT, e));
    throw new RuntimeException(e);
}
```

If either of the Publisher or Formatter are down, our client app will report the error to Jaeger. Jaeger will highlight all such errors in the UI corresponding to the failed span.

### Instrumenting the Servers

Our servers are currently not instrumented for tracing. We need to do the following:

#### Add some imports

For the code snippets we are adding, we need a few extra imports:

```java
import java.util.HashMap;

import javax.ws.rs.core.Context;
import javax.ws.rs.core.HttpHeaders;
import javax.ws.rs.core.MultivaluedMap;

import com.google.common.collect.ImmutableMap;

import io.opentracing.Scope;
import io.opentracing.SpanContext;
import io.opentracing.Tracer;
import io.opentracing.propagation.Format;
import io.opentracing.propagation.TextMapAdapter;
import io.opentracing.tag.Tags;
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

First, let's add a helper function on the Formatter class:

```java
public static Span startServerSpan(Tracer tracer, javax.ws.rs.core.HttpHeaders httpHeaders, String operationName) {
    // format the headers for extraction
    MultivaluedMap<String, String> rawHeaders = httpHeaders.getRequestHeaders();
    final HashMap<String, String> headers = new HashMap<String, String>();
    for (String key : rawHeaders.keySet()) {
        headers.put(key, rawHeaders.get(key).get(0));
    }

    Tracer.SpanBuilder spanBuilder;
    try {
        SpanContext parentSpanCtx = tracer.extract(Format.Builtin.HTTP_HEADERS, new TextMapAdapter(headers));
        if (parentSpanCtx == null) {
            spanBuilder = tracer.buildSpan(operationName);
        } else {
            spanBuilder = tracer.buildSpan(operationName).asChildOf(parentSpanCtx);
        }
    } catch (IllegalArgumentException e) {
        spanBuilder = tracer.buildSpan(operationName);
    }
    return spanBuilder.withTag(Tags.SPAN_KIND.getKey(), Tags.SPAN_KIND_SERVER).start();
}
```

The logic here is similar to the client side instrumentation, except that we are using `tracer.extract`
and tagging the span as `span.kind=server`. Instead of using a dedicated adapter class to convert
JAXRS `HttpHeaders` type into `io.opentracing.propagation.TextMap`, we are copying the headers to a plain
`HashMap<String, String>` and using a standard adapter `TextMapAdapter`.

It would be better to have this in a more appropriate place. We've prepared a `Tracing` class under the `lib`
package: that's what we'll be using in the future.

Now change the `FormatterResource` handler method to use `startServerSpan`:

```java
@GET
public String format(@QueryParam("helloTo") String helloTo, @Context HttpHeaders httpHeaders) {
    Span span = startServerSpan(tracer, httpHeaders, "format");
    try (Scope scope = tracer.scopeManager().activate(span)) {
        String helloStr = String.format("Hello, %s!", helloTo);
        span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
        return helloStr;
    } finally {
      span.finish();
    }
}
```

#### Apply the same to the Publisher

Now, just apply the same changes to the publisher.

### Take It For a Spin

As before, first run the `formatter` and `publisher` apps in separate terminals.
Then run `lesson03.exercise.Hello`. You should see the outputs like this:

```
# formatter
$ ./run.sh lesson03.exercise.Formatter server
[skip noise]
INFO  [2018-10-19 09:12:55,389] org.eclipse.jetty.server.Server: Started @2892ms
INFO  [2018-10-19 09:13:02,001] io.jaegertracing.internal.reporters.LoggingReporter: Span reported: ed5421da32d2cbe9:e7874d61d10a0c4d:9256da5294132c28:1 - format
127.0.0.1 - - [19/Oct/2018:09:13:02 +0000] "GET /format?helloTo=Bryan HTTP/1.1" 200 13 "-" "okhttp/3.9.0" 56

# publisher
$ ./run.sh lesson03.exercise.Publisher server
[skip noise]
INFO  [2018-10-19 09:12:56,636] org.eclipse.jetty.server.Server: Started @2619ms
Hello, Bryan!
INFO  [2018-10-19 09:13:02,173] io.jaegertracing.internal.reporters.LoggingReporter: Span reported: ed5421da32d2cbe9:8ac04690780c2b5c:ddc6239bde637c47:1 - format
127.0.0.1 - - [19/Oct/2018:09:13:02 +0000] "GET /publish?helloStr=Hello,%20Bryan! HTTP/1.1" 200 9 "-" "okhttp/3.9.0" 89

# client
$ ./run.sh lesson03.exercise.Hello Bryan
11:13:01.695 [main] INFO io.jaegertracing.Configuration - Initialized tracer=JaegerTracer(version=Java-0.32.0, serviceName=hello-world, ...)
11:13:02.035 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: ed5421da32d2cbe9:9256da5294132c28:ed5421da32d2cbe9:1 - formatString
11:13:02.190 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: ed5421da32d2cbe9:ddc6239bde637c47:ed5421da32d2cbe9:1 - printHello
11:13:02.190 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: ed5421da32d2cbe9:ed5421da32d2cbe9:0:1 - say-hello
```

Note how all recorded spans show the same trace ID `ed5421da32d2cbe9`. This is a sign
of correct instrumentation. It is also a very useful debugging approach when something
is wrong with tracing. A typical error is to miss the context propagation somewhere,
either in-process or inter-process, which results in different trace IDs and broken
traces.

If we open this trace in the UI, we should see all five spans.

![Trace](../../../../../go/lesson03/trace.png)

## Conclusion

The complete program can be found in the [solution](./solution) package.

Next lesson: [Baggage](../lesson04).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
