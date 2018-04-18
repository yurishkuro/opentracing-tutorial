using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTracing.Util;

namespace Lesson03.Exercise.Controllers
{
    [Route("api/format")]
    public class FormatController : Controller
    {
        // GET: api/format
        [HttpGet]
        public string Get()
        {
            return "Hello!";
        }

        // GET: api/format/helloString
        [HttpGet("{helloString}", Name = "GetFormat")]
        public string Get(string helloString)
        {
            var formattedHelloString = $"Hello {helloString}!";
            return formattedHelloString;
        }
    }
}
