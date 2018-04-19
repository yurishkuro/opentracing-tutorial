using Microsoft.AspNetCore.Mvc;
using OpenTracing.Tutorial.Library;
using System.Threading.Tasks;

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
        public async Task<string> Get(string helloString)
        {
            using (var tracer = Tracing.Init("hello-world"))
            {
                await new Hello(tracer).SayHello(helloString);
            }
            return helloString;
        }
    }
}
