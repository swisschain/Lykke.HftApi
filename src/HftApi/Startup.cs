﻿using System;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HftApi.Common.Configuration;
using HftApi.Common.Persistence;
using HftApi.GrpcServices;
using HftApi.Middleware;
using Lykke.HftApi.Domain.Services;
using Lykke.HftApi.Services;
using Microsoft.AspNetCore.Hosting;
using Swisschain.Sdk.Server.Common;

namespace HftApi
{
    public sealed class Startup : SwisschainStartup<AppConfig>
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
            AddJwtAuth(Config.Auth.JwtSecret, Config.Auth.LykkeAud);
        }

        protected override void ConfigureServicesExt(IServiceCollection services)
        {
            base.ConfigureServicesExt(services);

            services.AddPersistence(Config.Db.ConnectionString);
            services.AddSingleton(Config.Auth);
            services.AddMemoryCache();
            services.AddHttpClient<AssetsHttpClient>(client =>
            {
                client.BaseAddress = new Uri(Config.Services.AssetsServiceUrl);
            });

            services.AddSingleton<ICacheService, CacheService>();
        }

        protected override void RegisterEndpoints(IEndpointRouteBuilder endpoints)
        {
            base.RegisterEndpoints(endpoints);

            endpoints.MapGrpcService<MonitoringService>();
        }

        protected override void ConfigureMiddleware(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<UnhandledExceptionsMiddleware>();
        }

        protected override void ConfigureContainerExt(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModule(Config));
        }
    }
}
