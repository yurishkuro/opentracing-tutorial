# Extra Credit - Using Existing Open Source Instrumentation

Adding manual instrumentation to ASP.NET Core like we did in [Lesson 3](../lesson03)
is tedious. Fortunately, we don't need to do that because that instrumentation itself already exists
as open source module:

  * https://github.com/opentracing-contrib/csharp-netcore

There is also an open source module for instrumenting gRPC server and client code:

  * https://github.com/opentracing-contrib/csharp-grpc

For an extra credit, try to use these modules to avoid instrumenting your code manually.

