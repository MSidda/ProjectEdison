﻿using Edison.Common.Interfaces;
using Edison.Common.Config;
using Edison.Common;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Edison.IoTHubControllerService.Config;
using Edison.IoTHubControllerService.Helpers;
using Edison.IoTHubControllerService.Consumers;

namespace Edison.IoTHubControllerService
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
            services.Configure<ServiceBusOptions>(Configuration.GetSection("ServiceBus"));
            services.Configure<IoTHubControllerOptions>(Configuration.GetSection("IoTHubController"));

            services.AddMassTransit(c =>
            {
                c.AddConsumer<IoTCreateDeviceRequestedConsumer>();
                c.AddConsumer<IoTDeleteDeviceRequestedConsumer>();
                c.AddConsumer<IoTUpdateTwinDesiredRequestedConsumer>();
                c.AddConsumer<IoTLaunchDirectMethodRequestedConsumer>();
            });
            services.AddScoped<IoTCreateDeviceRequestedConsumer>();
            services.AddScoped<IoTDeleteDeviceRequestedConsumer>();
            services.AddScoped<IoTUpdateTwinDesiredRequestedConsumer>();
            services.AddScoped<IoTLaunchDirectMethodRequestedConsumer>();
            services.AddScoped<RegistryManagerHelper>();
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
                return context.Response.WriteAsync("DeviceManagement Service is running...");
            });
        }
    }
}
