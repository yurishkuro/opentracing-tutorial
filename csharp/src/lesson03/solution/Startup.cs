using Jaeger.Core.Samplers;
using Jaeger.Core.Transport;
using Jaeger.Transport.Thrift.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace OpenTracing.Tutorial.Lesson03.Solution
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddLogging(builder => {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
            });
            services.AddTransient<ISampler, ConstSampler>(ctx => new ConstSampler(true));
            services.AddTransient<ITransport, JaegerUdpTransport>();
            services.AddTransient<ITracingWrapper, TracingWrapper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime, ITracer tracer)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Register tracer for disposal:
            // TODO: This is NOT triggered when the browser is stopped. Only when closing by CTRL-C...
            applicationLifetime.ApplicationStopping.Register(() => ((IDisposable)tracer).Dispose());

            // Setup rest of the middlewares:
            app.UseMiddleware<TracingMiddleware>();
            app.UseMvcWithDefaultRoute();
        }
    }
}
