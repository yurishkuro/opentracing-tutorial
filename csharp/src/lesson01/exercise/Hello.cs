using System;
using Jaeger.Core;
using Jaeger.Core.Reporters;
using Jaeger.Transport.Thrift.Transport;
using Microsoft.Extensions.Logging;

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
            using (var tracer = Tracing.Init("say-hello"))
            {
                new Hello(tracer).SayHello(helloTo);
            }
        }
    }

    public static class Tracing
    {
        public static Tracer Init(string serviceName)
        {
            var loggerFactory = new LoggerFactory().AddConsole();
            var loggingReporter = new LoggingReporter(loggerFactory);
            var remoteReporter = new RemoteReporter.Builder(new JaegerUdpTransport())
                .WithLoggerFactory(loggerFactory)
                .Build();

            return new Tracer.Builder(serviceName)
                .WithLoggerFactory(loggerFactory)
                .WithReporter(new CompositeReporter(loggingReporter, remoteReporter))
                .Build();
        }
    }
}
