﻿using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Logging;

namespace HftApi
{
    public class Program
    {
        //TODO: fix ApplicationInformation.AppName in SDK
        public static string AppName = Assembly.GetEntryAssembly()?.GetName().Name;
        private sealed class RemoteSettingsConfig
        {
            public IReadOnlyCollection<string> RemoteSettingsUrls { get; set; }
        }

        public static void Main(string[] args)
        {
            Console.Title = "Lykke HftApi";

            var remoteSettingsConfig = ApplicationEnvironment.Config.Get<RemoteSettingsConfig>();

            using var loggerFactory = LogConfigurator.Configure("Lykke", remoteSettingsConfig.RemoteSettingsUrls ?? Array.Empty<string>());

            var logger = loggerFactory.CreateLogger<Program>();

            try
            {
                logger.LogInformation("Application is being started");

                CreateHostBuilder(loggerFactory, remoteSettingsConfig).Build().Run();

                logger.LogInformation("Application has been stopped");
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application has been terminated unexpectedly");
            }
        }

        private static IHostBuilder CreateHostBuilder(ILoggerFactory loggerFactory, RemoteSettingsConfig remoteSettingsConfig) =>
            new HostBuilder()
                .SwisschainService<Startup>(options =>
                {
                    options.UseLoggerFactory(loggerFactory);
                    options.AddWebJsonConfigurationSources(remoteSettingsConfig.RemoteSettingsUrls ?? Array.Empty<string>());
                });
    }
}
