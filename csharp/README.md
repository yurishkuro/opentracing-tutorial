# OpenTracing Tutorial - C#

## Installing

The tutorials are using CNCF Jaeger (https://github.com/jaegertracing/jaeger) as the tracing backend, 
[see here](../README.md) how to install it in a Docker image.

For running the code, you will need the [.NET Core SDK](https://www.microsoft.com/net/download). The code can be written with any text editor and run from the command line.

For easier development, use the free IDE [Visual Studio Code](https://code.visualstudio.com/Download?wt.mc_id=DotNet_Home) available for Windows, Linux and macOS. For full language support including smart code completion and debugging, get the [C# extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) for Visual Studio Code. Alternatively, if you already have Visual Studio 2017 or higher, .NET Core SDK is already included.

This repository uses NuGet to manage dependencies. All dependencies are already included in the example projects.

All subsequent commands in the tutorials should be executed in the corresponding project directory.

## Lessons

* [Lesson 01 - Hello World](./src/lesson01)
  * Instantiate a Tracer
  * Create a simple trace
  * Annotate the trace
* [Lesson 02 - Context and Tracing Functions](./src/lesson02)
  * Trace individual functions
  * Combine multiple spans into a single trace
  * Propagate the in-process context
* [Lesson 03 - Tracing RPC Requests](./src/lesson03)
  * Trace a transaction across more than one microservice
  * Pass the context between processes using `Inject` and `Extract`
  * Apply OpenTracing-recommended tags
* [Lesson 04 - Baggage](./src/lesson04)
  * Understand distributed context propagation
  * Use baggage to pass data through the call graph
* [Extra Credit](./src/extracredit)
  * Use existing open source instrumentation
