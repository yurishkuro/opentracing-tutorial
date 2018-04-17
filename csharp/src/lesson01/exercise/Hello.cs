using System;
using System.Collections.Generic;
using Jaeger.Core;
using Jaeger.Core.Reporters;
using Jaeger.Transport.Thrift.Transport;
using Microsoft.Extensions.Logging;

namespace OpenTracing.Tutorial.Lesson01.Exercise
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
            using (var tracer = InitTracer("say-hello"))
            {
                new Hello(tracer).SayHello(helloTo);
            }
        }

        private static Tracer InitTracer(string serviceName)
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
