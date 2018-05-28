using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Tutorial.Lesson03.Solution.Client
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly ILogger<Hello> _logger;
        private readonly WebClient _webClient = new WebClient();

        private Hello(ITracer tracer, ILoggerFactory loggerFactory)
        {
            _tracer = tracer;
            _logger = loggerFactory.CreateLogger<Hello>();
        }

        private string FormatString(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("format-string").StartActive(true))
            {
                var url = $"http://localhost:8081/api/format/{helloTo}";
                var span = _tracer.ActiveSpan
                    .SetTag(Tags.SpanKind, Tags.SpanKindClient)
                    .SetTag(Tags.HttpMethod, "GET")
                    .SetTag(Tags.HttpUrl, url);

                var dictionary = new Dictionary<string, string>();
                _tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(dictionary));
                foreach (var entry in dictionary)
                    _webClient.Headers.Add(entry.Key, entry.Value);
                
                var helloString = _webClient.DownloadString(url);
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

        private void SayHello(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("say-hello").StartActive(true))
            {
                scope.Span.SetTag("hello-to", helloTo);
                var helloString = FormatString(helloTo);
                PrintHello(helloString);
            }
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
