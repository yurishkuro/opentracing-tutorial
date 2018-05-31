using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloManual
    {
        private readonly ITracer _tracer;
        private readonly ILogger<HelloManual> _logger;

        public HelloManual(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<HelloManual>();
        }

        private string FormatString(ISpan rootSpan, string helloTo)
        {
            var span = _tracer.BuildSpan("format-string").AsChildOf(rootSpan).Start();
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
            var span = _tracer.BuildSpan("print-hello").AsChildOf(rootSpan).Start();
            try
            {
                _logger.LogInformation(helloString);
                span.Log("WriteLine");
            }
            finally
            {
                span.Finish();
            }
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);
            var helloString = FormatString(span, helloTo);
            PrintHello(span, helloString);
            span.Finish();
        }

		// TODO: Rename MainManual to Main to run it. Make sure that HelloActive.cs has MainActive instead of Main, otherwise it will not build!
        public static void MainManual(string[] args)
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
                    new HelloManual(tracer, loggerFactory).SayHello(helloTo);
                }
            }
        }
    }
}
