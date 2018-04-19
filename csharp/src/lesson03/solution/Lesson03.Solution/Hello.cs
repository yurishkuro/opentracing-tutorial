using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace Lesson03.Exercise
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly WebClient _webClient = new WebClient();

        public Hello(ITracer tracer)
        {
            _tracer = tracer;
        }

        public async Task<string> FormatString(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("FormatString").StartActive(true))
            {
                var url = $"http://localhost:56870/api/format/{helloTo}";
                var span = _tracer.ActiveSpan;
                Tags.SpanKind.Set(span, Tags.SpanKindClient);
                Tags.HttpMethod.Set(span, "GET");
                Tags.HttpUrl.Set(span, url.ToString());

                // TODO: Refactor into own helper method
                // Inject into header of httpClient:
                var dictionary = new Dictionary<string, string>();
                _tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(dictionary));
                foreach (var entry in dictionary)
                    _webClient.Headers.Add(entry.Key, entry.Value);

                var helloString = await _webClient.DownloadStringTaskAsync(url);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                });
                return helloString;
            }
        }

        public async Task PrintHello(string helloString)
        {
            using (var scope = _tracer.BuildSpan("PrintHello").StartActive(true))
            {
                var url = $"http://localhost:56870/api/publish/{helloString}";
                var publishString = await _webClient.DownloadStringTaskAsync(url);
                Console.WriteLine(publishString);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "WriteLine"
                });
            }
        }

        public async Task<IActionResult> SayHello(string helloTo)
        {
            using (var scope = _tracer.BuildSpan("SayHello").StartActive(true))
            {
                scope.Span.SetTag("hello-to", helloTo);
                var helloString = await FormatString(helloTo);
                await PrintHello(helloString);
            }

            return new OkResult();
        }
    }
}
