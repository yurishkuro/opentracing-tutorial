# Lesson 2 - Context and Tracing Functions

## Objectives

Learn how to:

* Trace individual functions
* Combine multiple spans into a single trace
* Propagate the in-process context

## Walkthrough

First, copy your work or the official solution from [Lesson 1](../lesson01) to `lesson02/exercise/Hello.java`.

### Tracing individual functions

In [Lesson 1](../lesson01) we wrote a program that creates a trace that consists of a single span.
That single span combined two operations performed by the program, formatting the output string
and printing it. Let's move those operations into standalone functions first:

```java
String helloStr = formatString(span, helloTo);
printHello(span, helloStr);
```

and the functions:

```java
private String formatString(Span span, String helloTo) {
    String helloStr = String.format("Hello, %s!", helloTo);
    span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
    return helloStr;
}

private void printHello(Span span, String helloStr) {
    System.out.println(helloStr);
    span.log(ImmutableMap.of("event", "println"));
}
```

Of course, this does not change the outcome. What we really want to do is to wrap each function into its own span.

```java
private  String formatString(Span rootSpan, String helloTo) {
    Span span = tracer.buildSpan("formatString").start();
    try {
        String helloStr = String.format("Hello, %s!", helloTo);
        span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
        return helloStr;
    } finally {
        span.finish();
    }
}

private void printHello(Span rootSpan, String helloStr) {
    Span span = tracer.buildSpan("printHello").start();
    try {
        System.out.println(helloStr);
        span.log(ImmutableMap.of("event", "println"));
    } finally {
        span.finish();
    }
}
```

Let's run it:

```
$ ./run.sh lesson02.exercise.Hello Bryan
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 5c8896b14a475325:5c8896b14a475325:0:1 - formatString
Hello, Bryan!
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 4d321526e688db82:4d321526e688db82:0:1 - printHello
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 77572ab6fda3b52f:77572ab6fda3b52f:0:1 - say-hello
```

We got three spans, but there is a problem here. The first hexadecimal segment of the output represents
Jaeger trace ID, yet they are all different. If we search for those IDs in the UI each one will represent
a standalone trace with a single span. That's not what we wanted!

What we really wanted was to establish causal relationship between the two new spans to the root
span started in `main()`. We can do that by passing an additional option `asChildOf` to the span builder:

```java
Span span = tracer.buildSpan("formatString").asChildOf(rootSpan).start();
```

If we think of the trace as a directed acyclic graph where nodes are the spans and edges are
the causal relationships between them, then the `ChildOf` option is used to create one such
edge between `span` and `rootSpan`. In the API the edges are represented by `SpanReference` type
that consists of a `SpanContext` and a label. The `SpanContext` represents an immutable, thread-safe
portion of the span that can be used to establish references or to propagate it over the wire.
The label, or `ReferenceType`, describes the nature of the relationship. `ChildOf` relationship
means that the `rootSpan` has a logical dependency on the child `span` before `rootSpan` can
complete its operation. Another standard reference type in OpenTracing is `FollowsFrom`, which
means the `rootSpan` is the ancestor in the DAG, but it does not depend on the completion of the
child span, for example if the child represents a best-effort, fire-and-forget cache write.

If we modify the `printHello` function accordingly and run the app, we'll see that all reported
spans now belong to the same trace:

```
$ ./run.sh lesson02.exercise.Hello Bryan
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: dfc93ecae4890fbb:a575db749dc9a88f:dfc93ecae4890fbb:1 - formatString
Hello, Bryan!
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: dfc93ecae4890fbb:406faa4bb0446653:dfc93ecae4890fbb:1 - printHello
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: dfc93ecae4890fbb:dfc93ecae4890fbb:0:1 - say-hello
```

We can also see that instead of `0` in the 3rd position the first two reported spans display
`dfc93ecae4890fbb`, which is the ID of the root span. The root span is reported last because
it is the last one to finish.

If we find this trace in the UI, it will show a proper parent-child relationship between the spans.

The complete version of this program can be found in [./solution/HelloManual.java](./solution/HelloManual.java).

### Propagate the in-process context

You may have noticed a few unpleasant side effects of our recent changes
  * we had to pass the Span object as the first argument to each function
  * we also had to write somewhat verbose try/finally code to finish the spans

OpenTracing API for Java provides a better way. Using thread-locals and the notion of an "active span",
we can avoid passing the span through our code and just access it via `tracer`.

```java
import io.opentracing.Scope;

private void sayHello(String helloTo) {
    Span span = tracer.buildSpan("say-hello").start();
    try (Scope scope = tracer.scopeManager().activate(span)) {
        span.setTag("hello-to", helloTo);
        
        String helloStr = formatString(helloTo);
        printHello(helloStr);
    } finally {
        span.finish();
    }
}

private  String formatString(String helloTo) {
    Span span = tracer.buildSpan("formatString").start();
    try (Scope scope = tracer.scopeManager().activate(span)) {
        String helloStr = String.format("Hello, %s!", helloTo);
        span.log(ImmutableMap.of("event", "string-format", "value", helloStr));
        return helloStr;
    } finally {
        span.finish();
    }
}

private void printHello(String helloStr) {
    Span span = tracer.buildSpan("printHello").start();
    try (Scope scope = tracer.scopeManager().activate(span)) {
        System.out.println(helloStr);
        span.log(ImmutableMap.of("event", "println"));
    } finally {
        span.finish();
    }
}
```

In the above code we're making the following changes:
  * `trace.scopeManager().active(span)` makes the given span the currently
    active span. Once the scope is closed, the previous scope becomes current,
    thus re-activating previously active span in the current thread.
  * `Scope` is auto-closable, which allows us to use try-with-resource syntax.
  * If there is already an active span, it will act as the parent to the span
    created by `buildSpan()`.

If we run this program, we will see that all three reported spans have the same trace ID.

## Conclusion

The two complete programs, `HelloManual` and `HelloActive`, can be found in the [solution](./solution) package.

Next lesson: [Tracing RPC Requests](../lesson03).
