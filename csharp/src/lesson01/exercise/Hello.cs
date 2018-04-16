using System;
using System.Data.SqlTypes;
using OpenTracing.Util;

namespace OpenTracing.Tutorial.Lesson01.Exercise
{
    internal class Hello
    {
        private readonly ITracer tracer;

        public Hello(OpenTracing.ITracer tracer)
        {
            this.tracer = tracer;
        }

        public void SayHello(string helloTo)
        {
            var span = tracer.BuildSpan("say-hello").Start();
            var helloString = $"Hello, {helloTo}!";
            Console.WriteLine(helloString);
            span.Finish();
        }

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            var helloTo = args[0];
            new Hello(GlobalTracer.Instance).SayHello(helloTo);
        }
    }
}
