using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson01.Solution
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly ILogger<Hello> _logger;

        public Hello(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<Hello>();
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);
            var helloString = $"Hello, {helloTo}!";
            span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                }
            );
            _logger.LogInformation(helloString);
            span.Log("WriteLine");
            span.Finish();
        }

        public static void Main(string[] args)
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
                    new Hello(tracer, loggerFactory).SayHello(helloTo);
                }
            }
        }
    }
}
