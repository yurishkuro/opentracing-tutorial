# Lesson 2 - Context and Tracing Functions

## Objectives

Learn how to:

* Trace individual functions
* Combine multiple spans into a single trace
* Propagate the in-process context

## Walkthrough

First, copy your work or the official solution from [Lesson 1](../lesson01) to `lesson02/exercise/Hello.cs`.

### Tracing individual functions

In [Lesson 1](../lesson01) we wrote a program that creates a trace that consists of a single span.
That single span combined two operations performed by the program, formatting the output string
and printing it. Let's move those operations into standalone functions first:

```csharp
var helloString = FormatString(span, helloTo);
PrintHello(span, helloString);
```

and the functions:

```csharp
private static string FormatString(ISpan span, string helloTo)
{
    var helloString = $"Hello, {helloTo}!";
    span.Log(new Dictionary<string, object>
    {
        [LogFields.Event] = "string.Format",
        ["value"] = helloString
    });
    return helloString;
}

private static void PrintHello(ISpan span, string helloString)
{
    Console.WriteLine(helloString);
    span.Log("WriteLine");
}
```

Of course, this does not change the outcome. What we really want to do is to wrap each function into its own span.

```csharp
using System.Reflection;

private string FormatString(ISpan rootSpan, string helloTo)
{
    var span = _tracer.BuildSpan("format-string").Start();
    try
    {
        var helloString = $"Hello, {helloTo}!";
        span.Log(new Dictionary<string, object>
        {
            [LogFields.Event] = "string.Format",
            ["value"] = helloString
        });
        return helloString;
    }
    finally
    {
        span.Finish();
    }
}

private void PrintHello(ISpan rootSpan, string helloString)
{
    var span = _tracer.BuildSpan("print-hello").Start();
    try
    {
        Console.WriteLine(helloString);
        span.Log("WriteLine");
    }
    finally
    {
        span.Finish();
    }
}
```

Let's run it:

```powershell
$ dotnet run Bryan

info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 4353438848052479496, "Low": 8059970555673150305 },
        ...
    },
    "References": [],
    ...
}
Hello, Bryan!
info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 2134861473790156628, "Low": 4422115797691393275 },
        ...
    },
    "References": [],
    ...
}
info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 12550591731891114008, "Low": 14472260298413271319 },
        ...
    },
    "References": [],
    ...
}
```

We got three spans, but there is a problem here. The output for each span shows the Jaeger trace ID, yet they 
are all different. If we search for those IDs in the UI each one will represent a standalone trace with a single 
span. That's not what we wanted!

What we really wanted was to establish causal relationship between the two new spans to the root
span started in `Main()`. We can do that by passing an additional option `AsChildOf` to the span builder:

```csharp
var span = _tracer.BuildSpan("format-string").AsChildOf(rootSpan).Start();
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

If we modify the `PrintHello` function and `FormatString` function accordingly and run the app, we'll see that 
all reported spans now belong to the same trace:

```powershell
$ dotnet run Bryan
info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 10551131348252046144, "Low": 4395131861836039341 },
        ...
    },
    "References": [
        {
        "Type": "child_of",
        "Context": {
            "TraceId": { "High": 10551131348252046144, "Low": 4395131861836039341 },
            ...
            }
        }
    ],
    ...
}
Hello, Bryan!
info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 10551131348252046144, "Low": 4395131861836039341 },
        ...
    },
    "References": [
        {
        "Type": "child_of",
        "Context": {
            "TraceId": { "High": 10551131348252046144, "Low": 4395131861836039341 },
            ...
            }
        }
    ],
    ...
}
info: Jaeger.Core.Reporters.LoggingReporter[0]
Reporting span:
{
    "Context": {
        "TraceId": { "High": 10551131348252046144, "Low": 4395131861836039341 },
        ...
    },
    "References": [],
    ...
}
```

We can also see for the first two reported spans the `References` array contains references of type `child_of` 
with the ID of the root span. The root span is reported last because it is the last one to finish.

If we find this trace in the UI, it will show a proper parent-child relationship between the spans.

The complete version of this program can be found in [./solution/HelloManual.cs](./solution/HelloManual.cs).

### Propagate the in-process context

You may have noticed a few unpleasant side effects of our recent changes
  * we had to pass the Span object as the first argument to each function
  * we also had to write somewhat verbose try/finally code to finish the spans

OpenTracing API for C# provides a better way. Using thread-locals and the notion of an "active span",
we can avoid passing the span through our code and just access it via `_tracer`.

```csharp
private string FormatString(string helloTo)
{
    using (var scope = _tracer.BuildSpan("format-string").StartActive(true))
    {
        var helloString = $"Hello, {helloTo}!";
        scope.Span.Log(new Dictionary<string, object>
        {
            [LogFields.Event] = "string.Format",
            ["value"] = helloString
        });
        return helloString;
    }
}

private void PrintHello(string helloString)
{
    using (var scope = _tracer.BuildSpan("print-hello").StartActive(true))
    {
        Console.WriteLine(helloString);
        scope.Span.Log(new Dictionary<string, object>
        {
            [LogFields.Event] = "WriteLine"
        });
    }
}

public void SayHello(string helloTo)
{
    using (var scope = _tracer.BuildSpan("say-hello").StartActive(true))
    {
        scope.Span.SetTag("hello-to", helloTo);
        var helloString = FormatString(helloTo);
        PrintHello(helloString);
    }
}
```

In the above code we're making the following changes:
  * We use `StartActive()` method of the span builder instead of `Start()`,
    which makes the span "active" by storing it in a thread-local storage.
  * `StartActive()` returns a `IScope` object instead of a `ISpan`. IScope is a container of the currently
    active span. We access the active span via `scope.Span`. Once the scope is closed, the previous
    scope becomes current, thus re-activating previously active span in the current thread.
  * `IScope` implements `IDisposable`, which allows us to use the `using` syntax.
  * The boolean parameter in `StartActive(true)` tells the Scope that once it is disposed it should
    finish the span it represents.
  * `StartActive()` automatically creates a `ChildOf` reference to the previously active span, so that
    we don't have to use `AsChildOf()` builder method explicitly.

If we run this program, we will see that all three reported spans have the same trace ID.

## Conclusion

The two complete programs, `HelloManual` and `HelloActive`, can be found in the [solution](./solution) package.

Next lesson: [Tracing RPC Requests](../lesson03).
