# Extra Credit - Using Existing Open Source Instrumentation

![Trace](../lesson03/trace.png)

In the trace screenshot we can see that the client-side spans are a lot larger than the corresponding
server-side spans. This might indicate some time spent in establishing the HTTP connections. There is
an open source library https://github.com/opentracing-contrib/go-stdlib that provides OpenTracing
instrumentation for Go's standard `net/http` components, including detailed tracing of the HTTP connection.

Try replacing the manual OpenTracing instrumentation we added in the lesson around HTTP calls with
the instrumentation available in this library.
