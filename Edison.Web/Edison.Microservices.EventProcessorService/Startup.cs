﻿using Edison.Common.Interfaces;
using Edison.Common.Config;
using Edison.Common;
using Edison.Core.Interfaces;
using Edison.Core.Config;
using Edison.Core;
using Edison.EventProcessorService.Consumers;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Edison.EventProcessorService
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
            services.AddOptions();
            services.EnableKubernetes();
            services.Configure<AppInsightsOptions>(Configuration.GetSection("ApplicationInsights"));
            services.Configure<RestServiceOptions>(Configuration.GetSection("RestService"));
            services.Configure<ServiceBusOptions>(Configuration.GetSection("ServiceBus"));
            services.AddMassTransit(c =>
            {
                c.AddConsumer<EventClusterCreateOrUpdateRequestedConsumer>();
                c.AddConsumer<EventClusterCloseRequestedConsumer>();
            });
            services.AddScoped<EventClusterCreateOrUpdateRequestedConsumer>();
            services.AddScoped<EventClusterCloseRequestedConsumer>();

            services.AddSingleton<IEventClusterRestService, EventClusterRestService>();
            services.AddSingleton<IServiceBusClient, RabbitMQServiceBus>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // mirror logger messages to AppInsights
            loggerFactory.AddApplicationInsights(app.ApplicationServices, LogLevel.Debug);
            app.ApplicationServices.GetService<IServiceBusClient>().Start(ep =>
            {
                ep.LoadFrom(app.ApplicationServices);
            });

            app.Run(context =>
            {
                return context.Response.WriteAsync("EventProcessor Service is running...");
            });
        }
    }
}
