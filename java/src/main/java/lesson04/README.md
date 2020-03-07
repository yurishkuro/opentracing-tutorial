# Lesson 4 - Baggage

## Objectives

* Understand distributed context propagation
* Use baggage to pass data through the call graph

### Walkthrough

In Lesson 3 we have seen how span context is propagated over the wire between different applications.
It is not hard to see that this process can be generalized to propagating more than just the tracing context.
With OpenTracing instrumentation in place, we can support general purpose _distributed context propagation_
where we associate some metadata with the transaction and make that metadata available anywhere in the
distributed call graph. In OpenTracing this metadata is called _baggage_, to highlight the fact that
it is carried over in-band with all RPC requests, just like baggage.

To see how it works in OpenTracing, let's take the application we built in Lesson 3. You can copy the source
code from [../lesson03/solution](../lesson03/solution) package, changing the package name to `lesson04.exercise`
afterwards:

```
cp src/main/java/lesson03/solution/*java src/main/java/lesson04/exercise
```

The `formatter` service takes the `helloTo` parameter and returns a string `Hello, {helloTo}!`. Let's modify
it so that we can customize the greeting too, but without modifying the public API of that service.

### Set Baggage in the Client

Let's add a new parameter to our Hello's main method, so that it accepts a `greeting` in addition to a name.
This is how the main method would look like in the end:

```java
public static void main(String[] args) {
    if (args.length != 2) {
        throw new IllegalArgumentException("Expecting two arguments, helloTo and greeting");
    }
    String helloTo = args[0];
    String greeting = args[1];
    Tracer tracer = Tracing.init("hello-world");
    new Hello(tracer).sayHello(helloTo, greeting);
}
```

Add a new `String greeting` parameter to the `sayHello` method and add this instruction to it, after starting the span:

```java
span.setBaggageItem("greeting", greeting);
```

By doing this we read a second command line argument as a "greeting" and store it in the baggage under `"greeting"` key.

### Read Baggage in Formatter

Add the following code to the `formatter`'s HTTP handler:

```java
String greeting = span.getBaggageItem("greeting");
if (greeting == null) {
    greeting = "Hello";
}
String helloStr = String.format("%s, %s!", greeting, helloTo);
```

### Run it

As in Lesson 3, first start the `formatter` and `publisher` in separate terminals, then run the client
with two arguments, e.g. `Bryan Bonjour`. The `publisher` should print `Bonjour, Bryan!`.

```
# formatter
$ ./run.sh lesson04.exercise.Formatter server
[skip noise]
INFO  [2018-10-19 09:41:19,267] org.eclipse.jetty.server.Server: Started @2456ms
INFO  [2018-10-19 09:41:32,373] io.jaegertracing.internal.reporters.LoggingReporter: Span reported: 7e19675d346d5019:7f79bb6597052eac:9fc7cbcf281445d3:1 - format
127.0.0.1 - - [19/Oct/2018:09:41:32 +0000] "GET /format?helloTo=Bryan HTTP/1.1" 200 15 "-" "okhttp/3.9.0" 53

# publisher
$ ./run.sh lesson04.exercise.Publisher server
[skip noise]
INFO  [2018-10-19 09:41:26,507] org.eclipse.jetty.server.Server: Started @2266ms
Bonjour, Bryan!
INFO  [2018-10-19 09:41:32,507] io.jaegertracing.internal.reporters.LoggingReporter: Span reported: 7e19675d346d5019:e77c64c2f8cb9c94:d966ef9a6a1c035b:1 - publish
127.0.0.1 - - [19/Oct/2018:09:41:32 +0000] "GET /publish?helloStr=Bonjour,%20Bryan! HTTP/1.1" 200 9 "-" "okhttp/3.9.0" 61

# client
$ ./run.sh lesson04.exercise.Hello Bryan Bonjour
11:41:32.068 [main] INFO io.jaegertracing.Configuration - Initialized tracer=JaegerTracer(version=Java-0.32.0, serviceName=hello-world, ...)
11:41:32.405 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 7e19675d346d5019:9fc7cbcf281445d3:7e19675d346d5019:1 - formatString
11:41:32.520 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 7e19675d346d5019:d966ef9a6a1c035b:7e19675d346d5019:1 - printHello
11:41:32.520 [main] INFO io.jaegertracing.internal.reporters.LoggingReporter - Span reported: 7e19675d346d5019:7e19675d346d5019:0:1 - say-hello
```

### What's the Big Deal?

We may ask - so what, we could've done the same thing by passing the `greeting` as an HTTP request parameter.
However, that is exactly the point of this exercise - we did not have to change any APIs on the path from
the root span in `Hello.java` all the way to the server-side span in `formatter`, three levels down.
If we had a much larger application with much deeper call tree, say the `formatter` was 10 levels down,
the exact code changes we made here would have worked, despite 8 more services being in the path.
If changing the API was the only way to pass the data, we would have needed to modify 8 more services
to get the same effect.

Some of the possible applications of baggage include:

  * passing the tenancy in multi-tenant systems
  * passing identity of the top caller
  * passing fault injection instructions for chaos engineering
  * passing request-scoped dimensions for other monitoring data, like separating metrics for prod vs. test traffic


### Now, a Warning... NOW a Warning?

Of course, while baggage is an extermely powerful mechanism, it is also dangerous. If we store a 1Mb value/string
in baggage, every request in the call graph below that point will have to carry that 1Mb of data. So baggage
must be used with caution. In fact, Jaeger client libraries implement centrally controlled baggage restrictions,
so that only blessed services can put blessed keys in the baggage, with possible restrictions on the value length.

## Conclusion

The complete program can be found in the [solution](./solution) package.

Extra Credit: [Using existing open source instrumentation](../extracredit).
