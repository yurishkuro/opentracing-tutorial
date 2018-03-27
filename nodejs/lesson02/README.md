# Lesson 2 - Context and Tracing Functions

## Objectives

Learn how to:

* Trace individual functions
* Combine multiple spans into a single trace
* Propagate the in-process context

## Walkthrough

First, copy your work or the official solution from [Lesson 1](../lesson01) to `lesson02/exercise/hello.js`:

```
mkdir lesson02/exercise
touch lesson02/exercise/hello.js
cp lesson01/solution/*.js lesson02/exercise/
```

### Tracing individual functions

In [Lesson 1](../lesson01) we wrote a program that creates a trace that consists of a single span.
That single span combined two operations performed by the program, formatting the output string
and printing it. Let's move those operations into standalone functions first:

```javascript
const sayHello = helloTo => {
  const span = tracer.startSpan("say-hello");
  span.setTag("hello-to", helloTo);
  const helloStr = formatString(span, helloTo);
  printString(span, helloStr);
  span.finish();
};

const formatString = (span, helloTo) => {
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  return helloStr;
};

const printString = (span, helloStr) => {
  console.log(helloStr);
  span.log({ event: "print-string" });
};
```

Of course, this does not change the outcome. What we really want to do is to wrap each function into its own span.

```javascript
const formatString = (rootSpan, helloTo) => {
  const span = tracer.startSpan("format");
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  span.finish();
  return helloStr;
};

const printString = (rootSpan, helloStr) => {
  const span = tracer.startSpan("consoleLog");
  console.log(helloStr);
  span.log({ event: "print-string" });
  span.finish();
};
```

Let's run it:

```
$ node lesson02/exercise/hello.js multipleSpans
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
INFO  Reporting span 7e40f96ecdaa395a:7e40f96ecdaa395a:0:1
Hello, multipleSpans!
INFO  Reporting span e29038503bfab9e5:e29038503bfab9e5:0:1
INFO  Reporting span e0908f7e4104c7f9:e0908f7e4104c7f9:0:1
```

<!--
The output in the console (above) is quite different from the output given in the Python tutorial:
```
$ python -m lesson02.exercise.hello Bryan
Initializing Jaeger Tracer with UDP reporter
Using sampler ConstSampler(True)
opentracing.tracer initialized to <jaeger_client.tracer.Tracer object at 0x10d0bcf10>[app_name=hello-world]
Reporting span a5224a80cebaee4:a5224a80cebaee4:0:1 hello-world.format
Hello, Bryan!
Reporting span 947f0ad168b588aa:947f0ad168b588aa:0:1 hello-world.println
Reporting span 7fe927d093e3e33c:7fe927d093e3e33c:0:1 hello-world.say-hello
```
which implies the node client is perhaps written with significant differences from python client?
-->

We got three spans, but there is a problem here. The first hexadecimal segment of the output represents Jaeger trace ID, yet they are all different. If we search for those IDs in the UI each one will represent a standalone trace with a single span. That's not what we wanted!

What we really wanted was to establish a causal relationship between the two new spans to the root span. We can do that by passing an additional option to the `startSpan` function:

```javascript
const formatString = (rootSpan, helloTo) => {
  const span = tracer.startSpan("format", { childOf: rootSpan });
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  span.finish();
  return helloStr;
};

const printString = (rootSpan, helloStr) => {
  const span = tracer.startSpan("consoleLog", { childOf: rootSpan });
  console.log(helloStr);
  span.log({ event: "print-string" });
  span.finish();
};
```

If we think of the trace as a directed acyclic graph where nodes are the spans and edges are the causal relationships between them, then the `childOf` option is used to create one such edge between `span` and `rootSpan`. In the API the edges are represented by `SpanReference` type that consists of a `SpanContext` and a label. The `SpanContext` represents an immutable, thread-safe portion of the span that can be used to establish references or to propagate it over the wire. The label, or `ReferenceType`, describes the nature of the relationship. `ChildOf` relationship means that the `rootSpan` has a logical dependency on the child `span` before `rootSpan` can complete its operation. Another standard reference type in OpenTracing is `FollowsFrom`, which means the `rootSpan` is the ancestor in the DAG, but it does not depend on the completion of the child span, for example if the child represents a best-effort, fire-and-forget cache write.

If we modify the `formatString` and `printString` functions accordingly and run the app, we'll see that all reported spans now belong to the same trace:

```
node lesson02/exercise/hello.js Kara
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
INFO  Reporting span f807cdcd1b44f817:9cb1e0041868bfcd:f807cdcd1b44f817:1
Hello, Kara!
INFO  Reporting span f807cdcd1b44f817:6d38799352b613ec:f807cdcd1b44f817:1
INFO  Reporting span f807cdcd1b44f817:f807cdcd1b44f817:0:1
```

We can also see that the first hexadecimal segment of the output for all three spans is `f807cdcd1b44f817`, which is the Jaeger trace ID of the root span. Additionally, the first two reported spans display the trace ID of the root span in the 3rd position, instead of `0`. The root span is reported last because it is the last one to finish.

If we find this trace in the UI, it will show a proper parent-child relationship between the spans.

### Propagate the in-process context

You may have noticed one unpleasant side effect of our recent changes - we had to pass the Span object as the first argument to each function. JavaScript does not support the notion of thread-local variables,
so in order to link the individual spans together we _do need to pass something_.
