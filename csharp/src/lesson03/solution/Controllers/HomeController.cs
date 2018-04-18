using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using OpenTracing.Propagation;
using OpenTracing.Tag;

namespace OpenTracing.Tutorial.Lesson03.Solution.Controllers
{
    public class HomeController : Controller
    {
        private readonly ITracer tracer;
        private readonly HttpClient httpClient = new HttpClient();

        public HomeController(ITracer tracer)
        {
            this.tracer = tracer;
        }

        public async Task<IActionResult> Index()
        {
            var helloTo = "Hello World";
            using (var scope = tracer.BuildSpan("say-hello").StartActive(true)) {
                scope.Span.SetTag("hello-to", helloTo);

                var helloStr = await FormatString(helloTo);
                await PrintHello(helloStr);
            }
            return Ok();
        }

        private Task<string> GetHttp(string endPoint, string param, string value)
        {
            var url = $"http://{Request.Host.Value}/{endPoint}?{param}={HttpUtility.UrlEncode(value)}";
            var span = tracer.ActiveSpan;
            Tags.SpanKind.Set(span, Tags.SpanKindClient);
            Tags.HttpMethod.Set(span, "GET");
            Tags.HttpUrl.Set(span, url);

            // Inject into header of httpClient:
            var dictionary = new Dictionary<string, string>();
            tracer.Inject(span.Context, BuiltinFormats.HttpHeaders, new TextMapInjectAdapter(dictionary));
            foreach (var entry in dictionary)
                httpClient.DefaultRequestHeaders.Add(entry.Key, entry.Value);

            return httpClient.GetStringAsync(url);
        }

        private async Task<string> FormatString(string helloTo)
        {
            using (var scope = tracer.BuildSpan("FormatString").StartActive(true))
            {
                var helloStr = await GetHttp("format", "helloTo", helloTo);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloStr
                });
                return helloStr;
            }
        }

        private async Task PrintHello(string helloStr)
        {
            using (var scope = tracer.BuildSpan("PrintHello").StartActive(true))
            {
                var result = await GetHttp("publish", "helloStr", helloStr);
                scope.Span.Log("println");
            }
        }
    }
}
