using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson03.Solution.Controllers
{
    [Route("format")]
    public class FormatController : Controller
    {
        private readonly ITracer tracer;

        public FormatController(ITracer tracer)
        {
            this.tracer = tracer;
        }

        [HttpGet]
        public string Get([FromQuery] string helloTo)
        {
            var headers = Request.Headers.ToDictionary(k => k.Key, v => v.Value.First());
            using (var scope = Tracing.StartServerSpan(tracer, headers, "format"))
            {
                var helloStr = $"Hello, {helloTo}";
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string-format",
                    ["value"] = helloStr
                });
                return helloStr;
            }
        }
    }
}
