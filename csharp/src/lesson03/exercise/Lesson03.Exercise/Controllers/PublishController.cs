using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpenTracing.Util;

namespace Lesson03.Exercise.Controllers
{
    [Route("api/publish")]
    public class PublishController : Controller
    {
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
            Console.WriteLine(helloString);
            return "published";
        }
    }
}
