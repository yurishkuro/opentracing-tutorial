using System;
using System.Collections.Generic;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson01.Solution
{
    internal class Hello
    {
        private readonly ITracer _tracer;

        public Hello(OpenTracing.ITracer tracer)
        {
            this._tracer = tracer;
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);
            var helloString = $"Hello, {helloTo}!";
            span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                }
            );
            Console.WriteLine(helloString);
            span.Log(new Dictionary<string, object>
            {
                [LogFields.Event] = "WriteLine"
            });
            span.Finish();
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
                new Hello(tracer).SayHello(helloTo);
            }
        }
    }
}
