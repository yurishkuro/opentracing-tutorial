# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

Let's create a simple Python program `lesson01/exercise/hello.py` that takes an argument and prints "Hello, {arg}!".

```
mkdir lesson01/exercise
touch lesson01/exercise/__init__.py
```

```python
# lesson01/exercise/hello.py
import sys

def say_hello(hello_to):
    hello_str = 'Hello, %s!' % hello_to
    print(hello_str)

assert len(sys.argv) == 2

hello_to = sys.argv[1]
say_hello(hello_to)
```

Run it:
```
$ python -m lesson01.exercise.hello Bryan
Hello, Bryan!
```

### Create a trace

A trace is a directed acyclic graph of spans. A span is a logical representation of some work done in your application.
Each span has these minimum attributes: an operation name, a start time, and a finish time.

Let's create a trace that consists of just a single span. To do that we need an instance of the `opentracing.Tracer`.
We can use a global instance stored in `opentracing.tracer`.

```python
import opentracing

tracer = opentracing.tracer

def say_hello(hello_to):
    span = tracer.start_span('say-hello')
    hello_str = 'Hello, %s!' % hello_to
    print(hello_str)
    span.finish()
```

We are using the following basic features of the OpenTracing API:
  * a `tracer` instance is used to start new spans via `start_span` function
  * each `span` is given an _operation name_, `'say-hello'` in this case
  * each `span` must be finished by calling its `finish()` function
  * the start and end timestamps of the span will be captured automatically by the tracer implementation

However, calling `finish()` manually is a bit tedious, we can use span as a context manager instead:

```python
def say_hello(hello_to):
    with tracer.start_span('say-hello') as span:
        hello_str = 'Hello, %s!' % hello_to
        print(hello_str)
```

If we run this program, we will see no difference, and no traces in the tracing UI.
That's because the variable `opentracing.tracer` points to a no-op tracer by default.

### Initialize a real tracer

Let's create an instance of a real tracer, such as Jaeger (http://github.com/uber/jaeger-client-python).

```python
import logging
from jaeger_client import Config


def init_tracer(service):
    logging.getLogger('').handlers = []
    logging.basicConfig(format='%(message)s', level=logging.DEBUG)

    config = Config(
        config={
            'sampler': {
                'type': 'const',
                'param': 1,
            },
            'logging': True,
        },
        service_name=service,
    )

    # this call also sets opentracing.tracer
    return config.initialize_tracer()
```

To use this instance, let's replace `opentracing.tracer` with `init_tracer(...)`:

```python
tracer = init_tracer('hello-world')
```

Note that we are passing a string `'hello-world'` to the init method. It is used to mark all spans emitted by
the tracer as originating from a `hello-world` service.

There's one more thing we need to do. Jaeger Tracer is primarily designed for long-running server processes,
so it has an internal buffer of spans that is flushed by a background thread. Since our program exists immediately,
it may not have time to flush the spans to Jaeger backend. Let's add the following to the end of `hello.py`:

```
import time

# ...

# yield to IOLoop to flush the spans
time.sleep(2)
tracer.close()
```

If we run the program now, we should see a span logged:

```
$ python -m lesson01.exercise.hello Bryan
Initializing Jaeger Tracer with UDP reporter
Using sampler ConstSampler(True)
opentracing.tracer initialized to <jaeger_client.tracer.Tracer object at 0x110342f10>[app_name=hello-world]
Hello, Bryan!
Reporting span 5d42508ffcc40cc1:5d42508ffcc40cc1:0:1 hello-world.say-hello
```

If you have Jaeger backend running, you should be able to see the trace in the UI.

### Annotate the Trace with Tags and Logs

Right now the trace we created is very basic. If we call our program with argument `Susan`
instead of `Bryan`, the resulting traces will be nearly identical. It would be nice if we could
capture the program arguments in the traces to distinguish them.

One naive way is to use the string `"Hello, Bryan!"` as the _operation name_ of the span, instead of `"say-hello"`.
However, such practice is highly discouraged in distributed tracing, because the operation name is meant to
represent a _class of spans_, rather than a unique instance. For example, in Jaeger UI you can select the
operation name from a dropdown when searching for traces. It would be very bad user experience if we ran the
program to say hello to a 1000 people and the dropdown then contained 1000 entries. Another reason for choosing
more general operation names is to allow the tracing systems to do aggregations. For example, Jaeger tracer
has an option of emitting metrics for all the traffic going through the application. Having a unique
operation name for each span would make the metrics useless.

The recommended solution is to annotate spans with tags or logs. A _tag_ is a key-value pair that provides
certain metadata about the span. A _log_ is similar to a regular log statement, it contains
a timestamp and some data, but it is associated with span from which it was logged.

When should we use tags vs. logs?  The tags are meant to describe attributes of the span that apply
to the whole duration of the span. For example, if a span represents an HTTP request, then the URL of the
request should be recorded as a tag because it does not make sense to think of the URL as something
that's only relevant at different points in time on the span. On the other hand, if the server responded
with a redirect URL, logging it would make more sense since there is a clear timestamp associated with such
event. The OpenTracing Specification provides guidelines called [Semantic Conventions][semantic-conventions]
for recommended tags and log fields.

#### Using Tags

In the case of `hello Bryan`, the string "Bryan" is a good candidate for a span tag, since it applies
to the whole span and not to a particular moment in time. We can record it like this:

```python
with tracer.start_span('say-hello') as span:
    span.set_tag('hello-to', hello_to)
```

#### Using Logs

Our hello program is so simple that it's difficult to find a relevant example of a log, but let's try.
Right now we're formatting the `hello_str` and then printing it. Both of these operations take certain
time, so we can log their completion:

```python
hello_str = 'Hello, %s!' % hello_to
span.log_kv({'event': 'string-format', 'value': hello_str})

print(hello_str)
span.log_kv({'event': 'println'})
```

The log statements might look a bit strange if you have not previosuly worked with a structured logging API.
Rather than formatting a log message into a single string that is easy for humans to read, structured
logging APIs encourage you to separate bits and pieces of that message into key-value pairs that can be
automatically processed by log aggregation systems. The idea comes from the realization that today most
logs are processed by machines rather than humans. Just [google "structured-logging"][google-logging]
for many articles on this topic.

The OpenTracing API for Python exposes structured logging API method `log_kv` that takes a dictionary
of key-value pairs.

The OpenTracing Specification also recommends all log statements to contain an `event` field that
describes the overall event being logged, with other attributes of the event provided as additional fields.

If you run the program with these changes, then find the trace in the UI and expand its span (by clicking on it),
you will be able to see the tags and logs.

## Conclusion

The complete program can be found in the [solution](./solution) package. We moved the `init_tracer`
helper function into its own package so that we can reuse it in the other lessons as `from lib.tracing import init_tracer`.

Next lesson: [Context and Tracing Functions](../lesson02).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
[google-logging]: https://www.google.com/search?q=structured-logging