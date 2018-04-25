using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using OpenTracing;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson03.Solution.Server.Controllers
{
    [Route("api/Format")]
    public class FormatController : Controller
    {
        private readonly ITracer _tracer;

        public FormatController(ITracer tracer)
        {
            _tracer = tracer;
        }

        // GET: api/Format
        [HttpGet]
        public string Get()
        {
            return "Hello!";
        }

        // GET: api/Format/helloString
        [HttpGet("{helloString}", Name = "GetFormat")]
        public string Get(string helloString)
        {
            using (var scope = _tracer.BuildSpan("format-controller").StartActive(true))
            {
                var formattedHelloString = $"Hello, {helloString}!";
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string-format",
                    ["value"] = formattedHelloString
                });
                return formattedHelloString;
            }
        }
    }
}