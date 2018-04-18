using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;
using OpenTracing.Tutorial.Library;

namespace Lesson03.Exercise.Controllers
{
    [Route("api/publish")]
    public class PublishController : Controller
    {
        private readonly ITracer _tracer;

        public PublishController(ITracer tracer)
        {
            _tracer = tracer;
        }

        // GET: api/publish
        [HttpGet]
        public string Get()
        {
            return "Hello!";
        }

        // GET: api/publish/helloString
        [HttpGet("{helloString}", Name = "GetPublish")]
        public string Get(string helloString)
        {
            var headers = Request.Headers.ToDictionary(k => k.Key, v => v.Value.First());
            using (var scope = Tracing.StartServerSpan(_tracer, headers, "PublishController"))
            {
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "WriteLine",
                    ["value"] = helloString
                });
                return "published";
            }
        }
    }
}
