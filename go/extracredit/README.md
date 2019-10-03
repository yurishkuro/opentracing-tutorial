# Extra Credit - Using Existing Open Source Instrumentation

![Trace](../lesson03/trace.png)

In the trace screenshot we can see that the client-side spans are a lot larger than the corresponding
server-side spans. This might indicate some time spent in establishing the HTTP connections. There is
an open source library that provides OpenTracing instrumentation for Go's standard `net/http` components,
including detailed tracing of the HTTP connection:

  * https://github.com/opentracing-contrib/go-stdlib

Try replacing the manual OpenTracing instrumentation we added in [Lesson 3](../lesson03) around HTTP calls
with the instrumentation available in this library.
