using System;
using System.Collections.Generic;
using System.Net;
using OpenTracing.Tutorial.Library;
using OpenTracing;

namespace Lesson03.Exercise.Client
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly WebClient _webClient = new WebClient();

        private Hello(ITracer tracer)
        {
            _tracer = tracer;
        }

        private string FormatString(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("FormatString").StartActive(true))
            {
                var url = $"http://localhost:56870/api/format/{helloTo}";
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
            using (var scope = _tracer.BuildSpan("PrintHello").StartActive(true))
            {
                Console.WriteLine(helloString);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "WriteLine"
                });
            }
        }

        private void SayHello(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("SayHello").StartActive(true))
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

            var helloTo = args[0];
            using (var tracer = Tracing.Init("say-hello"))
            {
                new Hello(tracer).SayHello(helloTo);
            }
        }
    }
}
