using Jaeger.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTracing.Tutorial.Library;
using OpenTracing.Util;

namespace OpenTracing.Tutorial.Lesson03.Solution
{
    public class Startup
    {
        private static readonly Tracer Tracer = Tracing.Init("Webservice");

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddLogging(builder => {
                builder.AddConfiguration(Configuration.GetSection("Logging"));
            });

            // Tutorial-Relevanter Teil:
            GlobalTracer.Register(Tracer);
            services.AddOpenTracing();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Register tracer for disposal:
            // TODO: This is NOT triggered when the browser is stopped. Only when closing by CTRL-C...
            applicationLifetime.ApplicationStopping.Register(Tracer.Dispose);

            app.UseMvcWithDefaultRoute();
        }
    }
}
