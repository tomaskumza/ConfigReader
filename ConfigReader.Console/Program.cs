using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TGW.ConfigReader.Interfaces;

namespace TGW.ConfigReader.Console
{
    class Program
    {
        private const string SERILOG_CONFIG_PATH = @".\Configs\serilog.json";

        static void Main(string[] args)
        {
            InitLogger();

            try
            {
                CreateHostBuilder(args).RunConsoleAsync().Wait();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "An unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilder()
               .ConfigureServices((hostContext, services) =>
                    {
                        services
                           .AddTransient<IFileSystem, FileSystem>()
                           .AddSingleton<IConsoleSetup, ConsoleSetup>()
                           .AddHostedService<ConsoleSetup>();
                    }
                )
               .ConfigureLogging((hostContext, logging) =>
                    {
                        logging.ClearProviders();
                        logging.AddSerilog();
                    }
                );

        private static void InitLogger()
        {
            if (!File.Exists(SERILOG_CONFIG_PATH))
                throw new ApplicationException($"Application config not found at {SERILOG_CONFIG_PATH}!");


            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile(SERILOG_CONFIG_PATH, false)
               
               .Build();

            Log.Logger = new LoggerConfiguration()
               .ReadFrom.Configuration(configuration)
               .CreateLogger();
        }
    }
}
