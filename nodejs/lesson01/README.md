**NOTE**

This README is currently incomplete / unfinished. Please refer to respective README in tutorials for one of the other languages

# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

Let's create a simple Node program `lesson01/exercise/hello.js` that takes an argument and prints "Hello, {arg}!".

```
mkdir lesson01/exercise
touch lesson01/exercise/hello.js
```

```javascript
# lesson01/exercise/hello.js
const assert = require("assert");

const sayHello = helloTo => {
  const helloStr = `Hello, ${helloTo}!`;
  console.log(helloStr);
}

assert.ok(process.argv.length == 3, 'expecting one argument');
const helloTo = process.argv[2];
sayHello(helloTo);
```

Run it:

```
node lesson01/exercise/hello.js Kara
Hello, Kara!
```

### Create a trace

A trace is a directed acyclic graph of spans. A span is a logical representation of some work done in your application.
Each span has these minimum attributes: an operation name, a start time, and a finish time.

Let's create a trace that consists of just a single span. To do that we need an instance of the `opentracing.Tracer`.
We can use a global instance stored in `opentracing.tracer`.

```javascript
const opentracing = require("opentracing");

const tracer = new opentracing.Tracer();

const sayHello = helloTo => {
  const span = tracer.startSpan("say_hello");
  const helloStr = `Hello, ${helloTo}!`;
  console.log(helloStr);
  span.finish();
};
```

We are using the following basic features of the OpenTracing API:

* a `tracer` instance is used to start new spans via the `startSpan` function
* each `span` is given an _operation name_, `"say_hello"` in this case
* each `span` must be finished by calling its `finish()` function
* the start and end timestamps of the span will be captured automatically by the tracer implementation

<!---
However, calling `finish()` manually is a bit tedious, we can use span as a context manager instead:
// Not yet implemented in the code snippet below
```javascript
const sayHello = helloTo => {
  const span = tracer.startSpan("say_hello");
  const helloStr = `Hello, ${helloTo}!`;
  console.log(helloStr);
};
```
--->

If we run this program, we will see no difference, and no traces in the tracing UI.
That's because the variable `new opentracing.Tracer()` points to a no-op tracer by default.

<!---
Run it:

```
node lesson01/solution/hello.js Peter
INFO  Initializing Jaeger Tracer with CompositeReporter and ConstSampler
Hello app listening on port 8080
```

Run the following curl command a few times:

```
curl localhost:8080
```

You should see something below on the console for the client app:

```
Hello, Peter!
INFO  Reporting span 6d8e165388a35fb5:6d8e165388a35fb5:0:1
Hello, Peter!
INFO  Reporting span 48b662d422dfcc86:48b662d422dfcc86:0:1
Hello, Peter!
INFO  Reporting span c0e45d92229168c5:c0e45d92229168c5:0:1
```

<---
