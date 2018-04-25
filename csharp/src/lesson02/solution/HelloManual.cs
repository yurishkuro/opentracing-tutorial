using System;
using System.Collections.Generic;
using OpenTracing.Tutorial.Library;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class HelloManual
    {
        private readonly ITracer _tracer;

        public HelloManual(ITracer tracer)
        {
            _tracer = tracer;
        }

        private string FormatString(ISpan rootSpan, string helloTo)
        {
            var span = _tracer.BuildSpan("format-string").AsChildOf(rootSpan).Start();
            try
            {
                var helloString = $"Hello, {helloTo}!";
                span.Log(new Dictionary<string, object>
                {
                    [LogFields.Event] = "string.Format",
                    ["value"] = helloString
                });
                return helloString;
            }
            finally
            {
                span.Finish();
            }
        }

        private void PrintHello(ISpan rootSpan, string helloString)
        {
            var span = _tracer.BuildSpan("print-hello").AsChildOf(rootSpan).Start();
            try
            {
                Console.WriteLine(helloString);
                span.Log("WriteLine");
            }
            finally
            {
                span.Finish();
            }
        }

        public void SayHello(string helloTo)
        {
            var span = _tracer.BuildSpan("say-hello").Start();
            span.SetTag("hello-to", helloTo);
            var helloString = FormatString(span, helloTo);
            PrintHello(span, helloString);
            span.Finish();
        }

        //public static void Main(string[] args)
        //{
        //    if (args.Length != 1)
        //    {
        //        throw new ArgumentException("Expecting one argument");
        //    }

        //    var helloTo = args[0];
        //    using (var tracer = Tracing.Init("hello-world"))
        //    {
        //        new HelloManual(tracer).SayHello(helloTo);
        //    }
        //}
    }
}
