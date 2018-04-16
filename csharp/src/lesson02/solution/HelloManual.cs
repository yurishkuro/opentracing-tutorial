using System;
using System.Collections.Generic;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloManual
    {
        private readonly ITracer tracer;

        public HelloManual(ITracer tracer)
        {
            this.tracer = tracer;
        }

        public void SayHello(String helloTo)
        {
            var span = tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);

            var helloStr = FormatString(span, helloTo);
            PrintHello(span, helloStr);

            span.Finish();
        }

        private string FormatString(ISpan rootSpan, string helloTo)
        {
            var span = tracer.BuildSpan("FormatString").AsChildOf(rootSpan).Start();
            try
            {
                var helloStr = $"Hello, {helloTo}!";
                span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloStr
                });
                return helloStr;
            }
            finally 
            {
                span.Finish();
            }
        }

        private void PrintHello(ISpan rootSpan, string helloStr)
        {
            var span = tracer.BuildSpan("PrintHello").AsChildOf(rootSpan).Start();
            try
            {
                Console.WriteLine(helloStr);
                span.Log("Console.WriteLine");
            }
            finally
            {
                span.Finish();
            }
        }
    }
}
