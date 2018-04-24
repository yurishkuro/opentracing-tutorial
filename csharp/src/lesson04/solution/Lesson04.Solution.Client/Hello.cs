using System;
using System.Collections.Generic;
using System.Net;
using OpenTracing.Tutorial.Library;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Tutorial.Lesson04.Solution.Client
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
            using (var scope = _tracer.BuildSpan("format-string").StartActive(true))
            {
                var url = $"http://localhost:56870/api/format/{helloTo}";
                var span = _tracer.ActiveSpan;
                Tags.SpanKind.Set(span, Tags.SpanKindClient);
                Tags.HttpMethod.Set(span, "GET");
                Tags.HttpUrl.Set(span, url);
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
                Console.WriteLine(helloString);
                scope.Span.Log("WriteLine");
            }
        }

        private void SayHello(string helloTo, string greeting)
        {
            using (var scope = _tracer.BuildSpan("say-hello").StartActive(true))
            {
                scope.Span.SetBaggageItem("greeting", greeting);
                scope.Span.SetTag("hello-to", helloTo);
                var helloString = FormatString(helloTo);
                PrintHello(helloString);
            }
        }

        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Expecting two arguments, helloTo and greeting");
            }

            var helloTo = args[0];
            var greeting = args[1];
            using (var tracer = Tracing.Init("hello-world"))
            {
                new Hello(tracer).SayHello(helloTo, greeting);
            }
        }
    }
}
