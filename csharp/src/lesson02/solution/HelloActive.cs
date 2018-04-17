using System;
using System.Collections.Generic;
using OpenTracing.Tutorial.Library;
using System.Reflection;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloActive
    {
        private readonly ITracer _tracer;

        public HelloActive(ITracer tracer)
        {
            _tracer = tracer;
        }

        private string FormatString(string helloTo)
        {
            using (var scope = _tracer.BuildSpan(MethodBase.GetCurrentMethod().Name).StartActive(true))
            {
                var helloString = $"Hello, {helloTo}!";
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                });
                return helloString;
            }
        }

        private void PrintHello(string helloString)
        {
            using (var scope = _tracer.BuildSpan(MethodBase.GetCurrentMethod().Name).StartActive(true))
            {
                Console.WriteLine(helloString);
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

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            var helloTo = args[0];
            using (var tracer = Tracing.Init("say-hello"))
            {
                new HelloActive(tracer).SayHello(helloTo);
            }
        }
    }
}
