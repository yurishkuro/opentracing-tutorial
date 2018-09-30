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
  printHello(span, helloStr);
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

const printHello = (span, helloStr) => {
  console.log(helloStr);
  span.log({ event: "print-string" });
};
```

Of course, this does not change the outcome. What we really want to do is to wrap each function into its own span.

```javascript
const formatString = (rootSpan, helloTo) => {
  const span = tracer.startSpan("formatString");
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  span.finish();
  return helloStr;
};

const printHello = (rootSpan, helloStr) => {
  const span = tracer.startSpan("printHello");
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

We got three spans, but there is a problem here. The first hexadecimal segment of the output represents Jaeger trace ID, yet they are all different. If we search for those IDs in the UI each one will represent a standalone trace with a single span. That's not what we wanted!

What we really wanted was to establish a causal relationship between the two new spans to the root span. We can do that by passing an additional option `childOf` to the `startSpan` function:

```javascript
const formatString = (rootSpan, helloTo) => {
  const span = tracer.startSpan("formatString", { childOf: rootSpan });
  const helloStr = `Hello, ${helloTo}!`;
  span.log({
    event: "string-format",
    value: helloStr,
  });
  span.finish();
  return helloStr;
};

const printHello = (rootSpan, helloStr) => {
  const span = tracer.startSpan("printHello", { childOf: rootSpan });
  console.log(helloStr);
  span.log({ event: "print-string" });
  span.finish();
};
```

If we think of the trace as a directed acyclic graph where nodes are the spans and edges are the causal relationships between them, then the `childOf` option is used to create one such edge between `span` and `rootSpan`. In the API the edges are represented by `SpanReference` type that consists of a `SpanContext` and a label. The `SpanContext` represents an immutable, thread-safe portion of the span that can be used to establish references or to propagate it over the wire. The label, or `ReferenceType`, describes the nature of the relationship. `ChildOf` relationship means that the `rootSpan` has a logical dependency on the child `span` before `rootSpan` can complete its operation. Another standard reference type in OpenTracing is `FollowsFrom`, which means the `rootSpan` is the ancestor in the DAG, but it does not depend on the completion of the child span, for example if the child represents a best-effort, fire-and-forget cache write.

If we modify the `formatString` and `printHello` functions accordingly and run the app, we'll see that all reported spans now belong to the same trace:

```
node lesson02/exercise/hello.js Bryan
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
INFO  Reporting span f807cdcd1b44f817:9cb1e0041868bfcd:f807cdcd1b44f817:1
Hello, Bryan!
INFO  Reporting span f807cdcd1b44f817:6d38799352b613ec:f807cdcd1b44f817:1
INFO  Reporting span f807cdcd1b44f817:f807cdcd1b44f817:0:1
```

We can also see that the first hexadecimal segment of the output for all three spans is `f807cdcd1b44f817`, which is the Jaeger trace ID of the root span. Additionally, the first two reported spans display the trace ID of the root span in the 3rd position, instead of `0`. The root span is reported last because it is the last one to finish.

If we find this trace in the UI, it will show a proper parent-child relationship between the spans.

### Propagate the in-process context

You may have noticed one unpleasant side effect of our recent changes - we had to pass the Span object as the first argument to each function. JavaScript does not support the notion of thread-local variables, so in order to link the individual spans together we _do need to pass something_. To avoid polluting the application with tracing code, we will create a context object in which to store the currently active span and pass that instead. The context object enables us to pass data, in addition to the span, that may be relevant to the application for the given request or transaction.

First, we need to create a context object, which we'll name `ctx`, in the main `sayHello()` function and store the span in it:

```javascript
const ctx = { span };
```

Then we pass the `ctx` object instead of the `rootSpan`:

```javascript
const helloStr = formatString(ctx, helloTo);
printHello(ctx, helloStr);
```

And we modify the `formatString()` and `printHello()` functions to reassign the value of the span property on the `ctx` object. We set the value of the span property to be a new span that is defined as a `childOf` the span property on the passed in `ctx` object:

```javascript
const formatString = (ctx, helloTo) => {
  ctx = {
    span: tracer.startSpan("formatString", { childOf: ctx.span }),
  };
  ...
}
const printHello = (ctx, helloStr) => {
    ctx = {
      span: tracer.startSpan("printHello", { childOf: ctx.span }),
    };
  ...
}
```

If we run this modified program, we will see that all three reported spans still have the same trace ID.

This `ctx` object gives us much greater control and flexibility in passing data between spans. If our functions were calling more functions, we could keep that context instance and pass it down, rather than passing the top-level context.

## Conclusion

The complete program can be found in the [solution](./solution) package.

Next lesson: [Tracing RPC Requests](../lesson03).
