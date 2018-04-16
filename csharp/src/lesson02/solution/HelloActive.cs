using System;
using System.Collections.Generic;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloActive
    {
        private readonly ITracer tracer;

        public HelloActive(ITracer tracer)
        {
            this.tracer = tracer;
        }

        public void SayHello(String helloTo)
        {
            using (var scope = tracer.BuildSpan("say-hello").StartActive(true))
            {
                scope.Span.SetTag("hello-to", helloTo);

                var helloStr = FormatString(helloTo);
                PrintHello(helloStr);
            }
        }

        private string FormatString(string helloTo)
        {
            using (var scope = tracer.BuildSpan("FormatString").StartActive(true))
            {
                var helloStr = $"Hello, {helloTo}!";
                scope.Span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloStr
                });
                return helloStr;
            }
        }

        private void PrintHello(string helloStr)
        {
            using (var scope = tracer.BuildSpan("PrintHello").StartActive(true))
            {
                Console.WriteLine(helloStr);
                scope.Span.Log("Console.WriteLine");
            }
        }
    }
}
