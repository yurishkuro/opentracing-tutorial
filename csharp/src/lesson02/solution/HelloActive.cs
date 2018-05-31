using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloActive
    {
        private readonly ITracer _tracer;
        private readonly ILogger<HelloActive> _logger;

        public HelloActive(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<HelloActive>();
        }

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
                _logger.LogInformation(helloString);
                scope.Span.Log("WriteLine");
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

		// TODO: Rename MainActive to Main to run it. Make sure that HelloManual.cs has MainManual instead of Main, otherwise it will not build!
        public static void MainActive(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            using (var loggerFactory = new LoggerFactory().AddConsole())
            {
                var helloTo = args[0];
                using (var tracer = Tracing.Init("hello-world", loggerFactory))
                {
                    new HelloActive(tracer, loggerFactory).SayHello(helloTo);
                }
            }
        }
    }
}
