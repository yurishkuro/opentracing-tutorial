using Microsoft.AspNetCore.Mvc;

namespace Lesson03.Exercise.Server.Controllers
{
    [Route("api/Format")]
    public class FormatController : Controller
    {
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
            var formattedHelloString = $"Hello, {helloString}!";
            return formattedHelloString;
        }
    }
}