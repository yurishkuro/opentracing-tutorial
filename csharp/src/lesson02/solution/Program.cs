using OpenTracing.Tutorial.Library;
using System;

namespace OpenTracing.Tutorial.Lesson02.Solution
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Expecting two argument");
            }

            var type = args[0];
            var helloTo = args[1];
            using (var tracer = Tracing.Init("hello-world"))
            {
                if (type == "Manual")
                    new HelloManual(tracer).SayHello(helloTo);
                else if (type == "Active")
                    new HelloActive(tracer).SayHello(helloTo);
                else
                    throw new ArgumentException("First argument has to be 'Manual' or 'Active'");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
