using Jaeger.Core;
using Jaeger.Core.Reporters;
using Jaeger.Transport.Thrift.Transport;
using Microsoft.Extensions.Logging;
using OpenTracing.Propagation;
using OpenTracing.Tag;
using System;
using System.Collections.Generic;

namespace OpenTracing.Tutorial.Library
{
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