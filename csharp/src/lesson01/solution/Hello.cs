using System;
using System.Collections.Generic;
using OpenTracing.Util;

namespace OpenTracing.Tutorial.Lesson01.Solution
{
    internal class Hello
    {
        private readonly ITracer tracer;

        public Hello(ITracer tracer)
        {
            this.tracer = tracer;
        }

        public void SayHello(String helloTo)
        {
            var span = tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);

            var helloStr = $"Hello, {helloTo}!";
            span.Log(new Dictionary<string, object>
            {
                [LogFields.Event] = "string.Format",
                ["value"] = helloStr
            });

            Console.WriteLine(helloStr);
            span.Log("Console.WriteLine");

            span.Finish();
        }
    }
}
