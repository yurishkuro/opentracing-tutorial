using Jaeger.Core;
using Jaeger.Core.Samplers;
using Jaeger.Core.Transport;
using Microsoft.AspNetCore.Hosting;

namespace OpenTracing.Tutorial.Lesson03.Solution
{
    public interface ITracingWrapper
    {
        IJaegerCoreTracer GetTracer();
    }

    public class TracingWrapper : ITracingWrapper
    {
        private readonly IJaegerCoreTracer _tracer;
        public TracingWrapper(IHostingEnvironment env, ITransport transport, ISampler sampler)
        {
            _tracer = new Tracer.Builder(env.ApplicationName)
                .WithTransport(transport)
                .WithSampler(sampler)
                .Build();
        }

        public IJaegerCoreTracer GetTracer() => _tracer;
    }
}
