# Lesson 1 - Hello World

## Objectives

Learn how to:

* Instantiate a Tracer
* Create a simple trace
* Annotate the trace

## Walkthrough

### A simple Hello-World program

Let's create a simple C# program `lesson01/exercise/Hello.cs` that takes an argument and prints `"Hello, {arg}!"`.

```csharp
using System;

namespace OpenTracing.Tutorial.Lesson01.Exercise
{
    internal class Hello
    {
        public void SayHello(string helloTo)
        {
            var helloString = $"Hello, {helloTo}!";
            Console.WriteLine(helloString);
        }
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            var helloTo = args[0];
            new Hello().SayHello(helloTo);
        }
    }
}
```

Run it:
```powershell
$ dotnet run Bryan
Hello, Bryan!
```

### Create a trace

A trace is a directed acyclic graph of spans. A span is a logical representation of some work done in your application.
Each span has these minimum attributes: an operation name, a start time, and a finish time.

Let's create a trace that consists of just a single span. To do that we need an instance of the `OpenTracing.ITracer`.
We can use a global instance returned by `OpenTracing.Util.GlobalTracer.Instance`.

```csharp
using System;
using OpenTracing.Util;

namespace OpenTracing.Tutorial.Lesson01.Exercise
{
    internal class Hello
    {
        private readonly ITracer _tracer;

        public Hello(OpenTracing.ITracer tracer)
        {
            _tracer = tracer;
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            var helloString = $"Hello, {helloTo}!";
            Console.WriteLine(helloString);
            span.Finish();
        }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            var helloTo = args[0];
            new Hello(GlobalTracer.Instance).SayHello(helloTo);
        }
    }
}
```

We are using the following basic features of the OpenTracing API:
  * a `ITracer` instance is used to create a span builder via `BuildSpan()`
  * each `ISpan` is given an _operation name_, `"say-hello"` in this case
  * builder is used to create a span via `Start()`
  * each `ISpan` must be finished by calling its `Finish()` function
  * the start and end timestamps of the span will be captured automatically by the tracer implementation

However, if we run this program, we will see no difference, and no traces in the tracing UI.
That's because the `GlobalTracer.Instance` returns a no-op tracer by default.

### Use `Microsoft.Extensions.Logging` for output

In the first draft, we used `Console.WriteLine` for the output. This works very well if there is no multi-threading. 
As soon as it gets more complex, our output could become unreadable if different sources write to our console at the same time.

Microsoft offers a framework for logging which has it's main target in the ASP.NET world, but also works fine with normal applications. 
You can find it as NuGet package `Microsoft.Extensions.Logging` which is already added to our project.

We need to create an `ILoggerFactory` instance in our `Main` method. `new LoggerFactory().AddConsole()` will create one that uses 
the `Console` as logging destination.

An `ILogger` instance can then be used for logging to that destination. `loggerFactory.CreateLogger<Hello>()` creates one that 
logs to our logging destination, using the `Hello` class as identifier. We store that in `_logger` for later usage.

`Console.WriteLine` can then be replaced by `_logger.LogInformation` which creates an information log.

```csharp
using System;

namespace OpenTracing.Tutorial.Lesson01.Exercise
{
    internal class Hello
    {
		...
        private readonly ILogger<Hello> _logger;

        public Hello(ITracer tracer, ILoggerFactory loggerFactory)
        {
            ...
            _logger = loggerFactory.CreateLogger<Hello>();
        }

        public void SayHello(string helloTo)
        {
            ...
            _logger.LogInformation(helloString);
            span.Log("WriteLine");
            span.Finish();
        }

        public static void Main(string[] args)
        {
            ...

            using (var loggerFactory = new LoggerFactory().AddConsole())
            {
                var helloTo = args[0];
                new Hello(GlobalTracer.Instance, loggerFactory).SayHello(helloTo);
            }
        }
    }
}
```

Run it:
```powershell
$ dotnet run Bryan
info: OpenTracing.Tutorial.Lesson01.Example.Hello[0]
	Hello, Bryan!
```

### Initialize a real tracer

Let's create an instance of a real tracer, such as Jaeger (https://github.com/jaegertracing/jaeger-client-csharp).

First let's define a helper function that will create a tracer.

```csharp
using Jaeger;
using Jaeger.Samplers;
using Microsoft.Extensions.Logging;

private static Tracer InitTracer(string serviceName, ILoggerFactory loggerFactory)
{
    var samplerConfiguration = new Configuration.SamplerConfiguration(loggerFactory)
        .WithType(ConstSampler.Type)
        .WithParam(1);

    var reporterConfiguration = new Configuration.ReporterConfiguration(loggerFactory)
        .WithLogSpans(true);

    return (Tracer)new Configuration(serviceName, loggerFactory)
        .WithSampler(samplerConfiguration)
        .WithReporter(reporterConfiguration)
        .GetTracer();
}
```

The Jaeger framework also uses the logging framework of our previous example, so we can just use the `ILoggerFactory` that 
we created earlier.

To use this instance, let's change the main function:

```csharp
using (var tracer = InitTracer("hello-world", loggerFactory))
{
    new Hello(tracer, loggerFactory).SayHello(helloTo);
}
```

Note that we are passing a string `hello-world` to the init method. It is used to mark all spans emitted by
the tracer as originating from a `hello-world` service.

If we run the program now, we should see a span logged:

```powershell
$ dotnet run Bryan
info: Jaeger.Configuration[0]
	Initialized Jaeger.Tracer
info: OpenTracing.Tutorial.Lesson01.Example.Hello[0]
	Hello, Bryan!
info: Jaeger.Reporters.LoggingReporter[0]
	Span reported: ae1baaa10cf3f6dcde151ba025a76ea4:de151ba025a76ea4:0:1 - say-hello
```

If you have the Jaeger backend running, you should be able to see the trace in the UI.

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

```csharp
var span = _tracer.BuildSpan("say-hello").Start();
span.SetTag("hello-to", helloTo);
```

#### Using Logs

Our hello program is so simple that it's difficult to find a relevant example of a log, but let's try.
Right now we're formatting the `helloString` and then printing it. Both of these operations take certain
time, so we can log their completion:

```csharp
var helloString = $"Hello, {helloTo}!";
span.Log(new Dictionary<string, object>
    {
        [LogFields.Event] = "string.Format",
        ["value"] = helloString
    }
);
_logger.LogInformation(helloString);
span.Log("WriteLine");
```

The log statements might look a bit strange if you have not previously worked with a structured logging API.
Rather than formatting a log message into a single string that is easy for humans to read, structured
logging APIs encourage you to separate bits and pieces of that message into key-value pairs that can be
automatically processed by log aggregation systems. The idea comes from the realization that today most
logs are processed by machines rather than humans. Just [google "structured-logging"][google-logging]
for many articles on this topic.

The OpenTracing API for C# exposes the structured logging API by accepting a dictionary
in the form of a `Dictionary<string, object>`.

The OpenTracing Specification also recommends all log statements to contain an `event` field that
describes the overall event being logged, with other attributes of the event provided as additional fields. 
If only an `event` is to be used, the C# API offers an shorthand like showed with `span.Log("WriteLine");`.

If you run the program with these changes, then find the trace in the UI and expand its span (by clicking on it),
you will be able to see the tags and logs.

## Conclusion

The complete program can be found in the [solution](./solution) package. We moved the `InitTracer`
helper function into its own class `Tracing` so that we can reuse it in the other lessons as `Tracer.Init()`.

Next lesson: [Context and Tracing Functions](../lesson02).

[semantic-conventions]: https://github.com/opentracing/specification/blob/master/semantic_conventions.md
[google-logging]: https://www.google.com/search?q=structured-logging