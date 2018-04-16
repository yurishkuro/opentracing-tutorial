using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTracing.Tag;
using System.Globalization;
using System.Threading.Tasks;

namespace OpenTracing.Tutorial.Lesson03.Solution
{
    public class TracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ITracingWrapper _tracer;
        private readonly ILogger<TracingMiddleware> _logger;

        public TracingMiddleware(RequestDelegate next, ITracingWrapper tracer, ILogger<TracingMiddleware> logger)
        {
            _next = next;
            _tracer = tracer;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var operationName = $"{context.Request.Method.ToUpper()} {context.Request.Path}";

            if (operationName.EndsWith(".ico", true, CultureInfo.InvariantCulture) || operationName.EndsWith(".png", true, CultureInfo.InvariantCulture)) {
                _logger.LogInformation($"Ignoring ({operationName}). Will not create a span.");
                await _next(context);
                return;
            }

            _logger.LogInformation($"Starting a new span: {operationName}");
            
            var builder = _tracer.GetTracer().BuildSpan(operationName)
                .WithTag(Tags.SpanKind.Key, Tags.SpanKindServer);

            using (builder.StartActive(true))
            {
                await _next(context);
                _logger.LogInformation($"Finishing span: {operationName}");
            }
        }
    }

}
