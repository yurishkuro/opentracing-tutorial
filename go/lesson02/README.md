# Lesson 2 - Context and Tracing Functions

## Objectives

Learn how to:

* Trace individual functions
* Combine multiple spans into a single trace
* Propagate the in-process context

## Walkthrough

### Tracing individual functions

In [Lesson 1](../lesson01) we wrote a program that creates a trace that consists of a single span.
That single span combined two operations performed by the propgram, formatting the output string
and printing it. Let's move those operations into standalone functions first:

```go
span := tracer.StartSpan("say-hello")
span.SetTag("hello-to", helloTo)
defer span.Finish()

helloStr := formatString(span, helloTo)
printHello(span, helloStr)
```

and the functions:

```go
func formatString(span opentracing.Span, helloTo string) string {
    helloStr := fmt.Sprintf("Hello, %s!", helloTo)
    span.LogFields(
        log.String("event", "string-format"),
        log.String("value", helloStr),
    )

    return helloStr
}

func printHello(span opentracing.Span, helloStr string) {
    println(helloStr)
    span.LogKV("event", "println")
}
```

Of course, this does not change the outcome. What we really want to do is to wrap each function into its own span.

```go
func formatString(rootSpan opentracing.Span, helloTo string) string {
    span := rootSpan.Tracer().StartSpan("formatString")
    defer span.Finish()

    helloStr := fmt.Sprintf("Hello, %s!", helloTo)
    span.LogFields(
        log.String("event", "string-format"),
        log.String("value", helloStr),
    )

    return helloStr
}

func printHello(rootSpan opentracing.Span, helloStr string) {
    span := rootSpan.Tracer().StartSpan("printHello")
    defer span.Finish()

    println(helloStr)
    span.LogKV("event", "println")
}
```

Let's run it:

```shell
$ go run ./lesson02/solution/hello.go Bryan
2017/09/24 14:56:04 Initializing logging reporter
2017/09/24 14:56:04 Reporting span 292bd18774533232:292bd18774533232:0:1
Hello, Bryan!
2017/09/24 14:56:04 Reporting span 2004e24c3362725f:2004e24c3362725f:0:1
2017/09/24 14:56:04 Reporting span 273d83da9cdc6413:273d83da9cdc6413:0:1
```

There is a problem here. The first hexadecimal segment of the output represents Jaeger trace ID,
but they are all different. If we search for those IDs in the UI each will represent a standalone
trace with a single span. That's not what we wanted!

What we really wanted was to establish causal relationship between the two new spans to the root
span started in `main()`. We can do that by passing an additional option to the `StartSpan`
function:

```go
    span = rootSpan.Tracer().StartSpan(
        "formatString",
        opentracing.ChildOf(span.Context()),
    )
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
child span.

If we modify the `printHello` function accordingly and run the app, we'll see that all reported
spans now belong to the same trace:

```shell
$ go run ./lesson02/solution/hello.go Bryan
2017/09/24 15:10:34 Initializing logging reporter
2017/09/24 15:10:34 Reporting span 479fefe9525eddb:2a66575ec4945eef:479fefe9525eddb:1
Hello, Bryan!
2017/09/24 15:10:34 Reporting span 479fefe9525eddb:5adb976bfc1f95c1:479fefe9525eddb:1
2017/09/24 15:10:34 Reporting span 479fefe9525eddb:479fefe9525eddb:0:1
```

We can also see that instead of `0` in the 3rd position the first two reported spans display
`479fefe9525eddb`, which is the ID of the root span. The root span is reported last because
it is the last one to finish.

If we find this trace in the UI, it will show a proper parent-child relationship between the spans.

### Propagate the in-process context

You may have noticed one unpleasant side effect of our recent changes - we had to pass the Span object
as the first argument to each function. Go langauges does not support the notion of thread-local variables,
so in order to link the individual spans together we _do need to pass something_. We just don't want that
to be the span object, since it pollutes the application with tracing code. The Go stardard library has
a type specifically designed for propagating request context throughout the application, called
`context.Context`. In addition to handling things like deadlines and cancellations, the Context
allows storing arbitrary key-value pairs, so we can use it to store the currently active span.
The OpenTracing API integrates with `context.Context` and provides convenient helper functions.

First we need to create the context in the `main()` function and store the span in it:

```go
ctx := context.Background()
ctx = opentracing.ContextWithSpan(ctx, span)
```

Then we pass the `ctx` object instead of the `rootSpan`:

```go
helloStr := formatString(ctx, helloTo)
printHello(ctx, helloStr)
```

And we modify the functions to use `StartSpanFromContext` helper function:

```go
func formatString(ctx context.Context, helloTo string) string {
    span, _ := opentracing.StartSpanFromContext(ctx, "formatString")
    defer span.Finish()
    ...

func printHello(ctx context.Context, helloStr string) {
    span, _ := opentracing.StartSpanFromContext(ctx, "printHello")
    defer span.Finish()
    ...
```

Note that we ignore the second value returned by the function, which is another instance of the Context
with the new span stored in it. If our functions were calling more functions, we could keep that Context
instance and pass it down, rather than passing the top-level context.

And one last thing. The `StartSpanFromContext` function uses `opentracing.GlobalTracer()` to start the
new spans, so we need to initialize that global variable to our instance of Jaeger tracer:

```go
tracer, closer := jaeger.Init("hello-world")
defer closer.Close()
opentracing.SetGlobalTracer(tracer)
```

## Conclusion

The complete program can be found in the [solution](./solution) package. 

Next lesson: [Tracing RPC Requests](../lesson03).
