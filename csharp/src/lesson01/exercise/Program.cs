using OpenTracing.Tutorial.Library;
using System;

namespace OpenTracing.Tutorial.Lesson01.Solution
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("Expecting one argument");
            }

            var helloTo = args[0];
            using (var tracer = Tracing.Init("hello-world"))
            {
                new Hello(tracer).SayHello(helloTo);
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
