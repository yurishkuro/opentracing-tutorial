using System;
using System.Collections.Generic;
using System.Web;
using System.Net;
using System.Net.Http;
using OpenTracing.Tutorial.Library;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using OpenTracing;

namespace Lesson03.Exercise
{
    internal class Hello
    {
        private readonly ITracer _tracer;
        private readonly WebClient webClient = new WebClient();

        public Hello(ITracer tracer)
        {
            _tracer = tracer;
        }

        public string FormatString(string helloTo)
        {
            using (var scope = _tracer.BuildSpan(MethodBase.GetCurrentMethod().Name).StartActive(true))
            {
                var url = $"http://localhost:56870/api/format/{helloTo}";
                var helloString = webClient.DownloadString(url);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                });
                return helloString;
            }
        }

        public void PrintHello(string helloString)
        {
            using (var scope = _tracer.BuildSpan(MethodBase.GetCurrentMethod().Name).StartActive(true))
            {
                var url = $"http://localhost:56870/api/publish/{helloString}";
                var publishString = webClient.DownloadString(url);
                Console.WriteLine(publishString);
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "WriteLine"
                });
            }
        }

        public void SayHello(string helloTo)
        {
            using (var scope = _tracer.BuildSpan(MethodBase.GetCurrentMethod().Name).StartActive(true))
            {
                scope.Span.SetTag("hello-to", helloTo);
                var helloString = FormatString(helloTo);
                PrintHello(helloString);
            }
        }

        //public static void Main(string[] args)
        //{
        //    if (args.Length != 1)
        //    {
        //        throw new ArgumentException("Expecting one argument");
        //    }

        //    var helloTo = args[0];
        //    using (var tracer = Tracing.Init("say-hello"))
        //    {
        //        new HelloActive(tracer).SayHello(helloTo);
        //    }
        //}
    }
}
