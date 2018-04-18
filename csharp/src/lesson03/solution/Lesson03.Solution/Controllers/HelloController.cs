using Microsoft.AspNetCore.Mvc;
using OpenTracing.Tutorial.Library;

namespace Lesson03.Exercise.Controllers
{
    [Route("api/hello")]
    public class HelloController : Controller
    {
        // GET: api/hello
        [HttpGet]
        public string Get()
        {
            return "Nothing to do. Please submit a hello string.";
        }

        // GET: api/hello/helloString
        [HttpGet("{helloString}", Name = "GetHello")]
        public string Get(string helloString)
        {
            using (var tracer = Tracing.Init("hello-world"))
            {
                new Hello(tracer).SayHello(helloString);
            }
            return helloString;
        }
    }
}
