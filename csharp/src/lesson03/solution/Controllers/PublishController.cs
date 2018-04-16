using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson03.Solution.Controllers
{
    [Route("publish")]
    public class PublishController : Controller
    {
        private readonly ITracer tracer;
        private ILogger<PublishController> logger;

        public PublishController(ITracer tracer, ILogger<PublishController> logger)
        {
            this.tracer = tracer;
            this.logger = logger;
        }

        [HttpGet]
        public string Get([FromQuery] string helloStr)
        {
            var headers = Request.Headers.ToDictionary(k => k.Key, v => v.Value.First());
            using (var scope = Tracing.StartServerSpan(tracer, headers, "format"))
            {
                logger.LogTrace(helloStr);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "println",
                    ["value"] = helloStr
                });
                return "published";
            }
        }
    }
}
