# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

Let's create a simple Go program `lesson01/hello.go` that takes an argument and prints "Hello, {arg}!". 

```go
package main

import (
    "fmt"
    "os"
)

func main() {
    if len(os.Args) != 2 {
        panic("ERROR: Expecting one argument")
    }
    helloTo := os.Args[1]
    helloStr := fmt.Sprintf("Hello, %s!", helloTo)
    println(helloStr)
}
```

Run it: 
```
$ go run ./lesson01/hello.go Bryan
Hello, Bryan!
```

### Create a trace

A trace is a directed acyclic graph of spans. A span is a logical representation of some work done in your application.
Each span has these minimum attributes: an operation name, a start time, and a finish time.

Let's create a trace that consists of just a single span. To do that we need an instance of the `opentracing.Tracer`.
We can use a global instance returned by `opentracing.GlobalTracer()`.

```go
tracer := opentracing.GlobalTracer()

span := tracer.StartSpan("say-hello")
println(helloStr)
span.Finish()
```

We are using the following basic features of the OpenTracing API:
  * a `tracer` instance is used to start new spans via `StartSpan` function
  * each `span` is given an _operation name_, `"say-hello"` in this case
  * each `span` must be finished by calling its `Finish()` function
  * the start and end timestamps of the span will be captured automatically by the tracer implementation

However, if we run this program, we will see no difference, and no traces in the tracing UI.
That's because the function `opentracing.GlobalTracer()` returns a no-op tracer by default.

### Initialize a real tracer

Let's create an instance of a real tracer, such as Jaeger (http://github.com/uber/jaeger-client-go).

```go
import (
	"fmt"
	"io"

	opentracing "github.com/opentracing/opentracing-go"
	jaeger "github.com/uber/jaeger-client-go"
	config "github.com/uber/jaeger-client-go/config"
)

// initJaeger returns an instance of Jaeger Tracer that samples 100% of traces and logs all spans to stdout.
func initJaeger(service string) (opentracing.Tracer, io.Closer) {
    cfg := &config.Configuration{
        Sampler: &config.SamplerConfig{
            Type:  "const",
            Param: 1,
        },
        Reporter: &config.ReporterConfig{
            LogSpans: true,
        },
    }
    tracer, closer, err := cfg.New(service, config.Logger(jaeger.StdLogger))
    if err != nil {
        panic(fmt.Sprintf("ERROR: cannot init Jaeger: %v\n", err))
    }
    return tracer, closer
}
```

To use this instance, let's change the main function:

```go
tracer, closer := initJaeger("hello-world")
defer closer.Close()
```

Note that we are passing a string `hello-world` to the init method. It is used to mark all spans emitted by
the tracer as originating from a `hello-world` service.

If we run the program now, we should see a span logged:

```
$ go run ./lesson01/hello.go Bryan
2017/09/22 20:26:49 Initializing logging reporter
Hello, Bryan!
2017/09/22 20:26:49 Reporting span 5642914c078ef2f0:5642914c078ef2f0:0:1
```

If you have Jaeger backend running, you should be able to see the trace in the UI.

### Annotate the Trace with Tags and Logs

Right now the trace we created is very basic. If we call our program with `hello.go Susan`
instead of `hello.go Bryan`, the resulting traces will be nearly identical. It would be nice if we could
capture the program arguments in the traces to distinguish them.

One naive way is to use the string `"Hello, Bryan!"` as the _operation name_ of the span, instead of `"say-hello"`.
However, such practice is highly discouraged in distributed tracing, because the operation name is meant to
represent a _class of spans_, rather than a unique instance. For example, in Jaeger UI you can select the
operation name from a dropdown when searching for traces. It would be very bad user experience if we ran the
program to say hello to a 1000 people and the dropdown then contained 1000 entries. Another reason for choosing
more general operation names is to allow the tracing systems to do aggregations. For example, Jaeger tracer
has an option of emitting metrics for all the traffic going through the application. Having a unique
operation name for each span would make the metrics useless.

The recommended solution is to annotate spans with tags or logs. A span _tag_ is a key-value pair that provides
certain metadata about the span. A span _log_ is pretty much the same as a regular log statement, it contains
a timestamp and some data.

When should we use tags vs. logs?  The tags are meant to describe attributes of the span that apply
to the whole duration of the span. For example, if a span represents an HTTP request, then the URL of the
request should be recorded as a tag because it does not make sense to think of the URL as something
that's only relevant at different points in time on the span. On the other hand, if the server responded
with a redirect URL, logging it would make more sense since there is a clear timestamp associated with such
event. The OpenTracing Specification provides guidelines called [Semantic Conventions][semantic-conventions]
for recommended tags and log fields.

#### Using Tags

In the case of `hello.go Bryan`, the string "Bryan" is a good candidate for a span tag, since it applies
to the whole span and not to a particular moment in time. We can record it like this:

```go
span := tracer.StartSpan("say-hello")
span.SetTag("hello-to", helloTo)
```

#### Using Logs

Our hello program is so simple that it's difficult to find a relevant example of a log, but let's try.
Right now we're formatting the `helloStr` and then printing it. Both of these operations take certain
time, so we can log their completion:

```go
helloStr := fmt.Sprintf("Hello, %s!", helloTo)
span.LogFields(
    log.String("event", "string-format"),
    log.String("value", helloStr),
)

println(helloStr)
span.LogKV("event", "println")
```

The log statements might look a bit strange if you have not previosuly worked with structured logging API.
Rather than formatting a log message into a single string that is easy for humans to read, structured
logging APIs encourage you to separate bits and pieces of that message into key-value pairs that can be
automatically processed by log aggregation systems. The idea comes from the realization that today most
logs are processed by machines rather than humans. Just [google "structured-logging"][google-logging]
for many articles on this topic.

The OpenTracing API for Go exposes structured logging API in two flavors:
  * The `LogFields` function takes strongly typed key-value pairs and is designed for zero-allocations
  * The `LogKV` function takes an alternating list of `key1,value1,key2,value2` pairs (easier to use)

The OpenTracing Specification also recommends all log statements to contain an `event` field that
describes the overall event being logged, with other attributes of the event provided as additional fields.

## Conclusion

The complete program can be found in the [solution](./solution) package. We moved the `initJaeger`
helper function into its own package so that we can reuse it in the other lessons as `jaeger.Init`.

Next lesson: [Context and Tracing Functions](../lesson02).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
[google-logging]: https://www.google.com/search?q=structured-logging