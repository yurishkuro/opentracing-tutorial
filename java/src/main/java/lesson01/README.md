# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

Let's create a simple Java program `lesson01/exercise/Hello.java` that takes an argument and prints `"Hello, {arg}!"`.

```java
package lesson01.exercise;

public class Hello {

    private void sayHello(String helloTo) {
        String helloStr = String.format("Hello, %s!", helloTo);
        System.out.println(helloStr);
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        new Hello().sayHello(helloTo);
    }
}
```

Run it:
```
$ ./run.sh lesson01.exercise.Hello Bryan
Hello, Bryan!
```

Here we're using a simple helper script `run.sh` that executes a class via Maven,
as well as strips out some of it diagnostic logging.

### Create a trace

A trace is a directed acyclic graph of spans. A span is a logical representation of some work done in your application.
Each span has these minimum attributes: an operation name, a start time, and a finish time.

Let's create a trace that consists of just a single span. To do that we need an instance of the `io.opentracing.Tracer`.
We can use a global instance returned by `io.opentracing.util.GlobalTracer.get()`.

```java
import io.opentracing.Span;
import io.opentracing.Tracer;
import io.opentracing.util.GlobalTracer;

public class Hello {

    private final Tracer tracer;

    private Hello(Tracer tracer) {
        this.tracer = tracer;
    }

    private void sayHello(String helloTo) {
        Span span = tracer.buildSpan("say-hello").start();

        String helloStr = String.format("Hello, %s!", helloTo);
        System.out.println(helloStr);

        span.finish();
    }

    public static void main(String[] args) {
        if (args.length != 1) {
            throw new IllegalArgumentException("Expecting one argument");
        }
        String helloTo = args[0];
        new Hello(GlobalTracer.get()).sayHello(helloTo);
    }
}
```

We are using the following basic features of the OpenTracing API:
  * a `tracer` instance is used to create a span builder via `buildSpan()`
  * each `span` is given an _operation name_, `"say-hello"` in this case
  * builder is used to create a span via `start()`
  * each `span` must be finished by calling its `finish()` function
  * the start and end timestamps of the span will be captured automatically by the tracer implementation

However, if we run this program, we will see no difference, and no traces in the tracing UI.
That's because the function `GlobalTracer.get()` returns a no-op tracer by default.

### Initialize a real tracer

Let's create an instance of a real tracer, such as Jaeger (https://github.com/jaegertracing/jaeger-client-java).
Our `pom.xml` already imports Jaeger:

```xml
<dependency>
    <groupId>io.jaegertracing</groupId>
    <artifactId>jaeger-client</artifactId>
    <version>0.32.0</version>
</dependency>
```

First let's define a helper function that will create a tracer.

```java
import io.jaegertracing.Configuration;
import io.jaegertracing.Configuration.ReporterConfiguration;
import io.jaegertracing.Configuration.SamplerConfiguration;
import io.jaegertracing.internal.JaegerTracer;

public static JaegerTracer initTracer(String service) {
    SamplerConfiguration samplerConfig = SamplerConfiguration.fromEnv().withType("const").withParam(1);
    ReporterConfiguration reporterConfig = ReporterConfiguration.fromEnv().withLogSpans(true);
    Configuration config = new Configuration(service).withSampler(samplerConfig).withReporter(reporterConfig);
    return config.getTracer();
}
```

To use this instance, let's change the main function:

```java
Tracer tracer = initTracer("hello-world");
new Hello(tracer).sayHello(helloTo);
```

Note that we are passing a string `hello-world` to the init method. It is used to mark all spans emitted by
the tracer as originating from a `hello-world` service.

If we run the program now, we should see a span logged:

```
$ ./run.sh lesson01.exercise.Hello Bryan
INFO io.jaegertracing.Configuration - Initialized tracer=JaegerTracer(version=Java-0.32.0, serviceName=hello-world, ...)
Hello, Bryan!
INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: aa206e9b64ca5f8b:aa206e9b64ca5f8b:0:1 - say-hello
```

If you have Jaeger backend running, you should be able to see the trace in the UI.

### Annotate the Trace with Tags and Logs

Right now the trace we created is very basic. If we call our program with `Hello Susan`
instead of `Hello Bryan`, the resulting traces will be nearly identical. It would be nice if we could
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

In the case of `Hello Bryan`, the string `"Bryan"` is a good candidate for a span tag, since it applies
to the whole span and not to a particular moment in time. We can record it like this:

```java
Span span = tracer.buildSpan("say-hello").start();
span.setTag("hello-to", helloTo);
```

#### Using Logs

Our hello program is so simple that it's difficult to find a relevant example of a log, but let's try.
Right now we're formatting the `helloStr` and then printing it. Both of these operations take certain
time, so we can log their completion:

```java
import com.google.common.collect.ImmutableMap;

// this goes inside the sayHello method
String helloStr = String.format("Hello, %s!", helloTo);
span.log(ImmutableMap.of("event", "string-format", "value", helloStr));

System.out.println(helloStr);
span.log(ImmutableMap.of("event", "println"));
```

The log statements might look a bit strange if you have not previosuly worked with a structured logging API.
Rather than formatting a log message into a single string that is easy for humans to read, structured
logging APIs encourage you to separate bits and pieces of that message into key-value pairs that can be
automatically processed by log aggregation systems. The idea comes from the realization that today most
logs are processed by machines rather than humans. Just [google "structured-logging"][google-logging]
for many articles on this topic.

The OpenTracing API for Java exposes structured logging API by accepting a collection of key-value pairs
in the form of a `Map<String, ?>`. Here we are using Guava's `ImmutableMap.of()` to construct such a map,
which takes an alternating list of `key1,value1,key2,value2` pairs.

The OpenTracing Specification also recommends all log statements to contain an `event` field that
describes the overall event being logged, with other attributes of the event provided as additional fields.

If you run the program with these changes, then find the trace in the UI and expand its span (by clicking on it),
you will be able to see the tags and logs.

## Conclusion

The complete program can be found in the [solution](./solution) package. We moved the `initTracer`
helper function into its own package `lib` so that we can reuse it in the other lessons as `Tracing.init()`.

Next lesson: [Context and Tracing Functions](../lesson02).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
[google-logging]: https://www.google.com/search?q=structured-logging
