using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using CommandLine;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using TeamServer.Interfaces;
using TeamServer.Models;

namespace TeamServer
{
    internal static class Program
    {
        private static IHandlerService _handlerService;
        private static IServerService _serverService;
        
        public static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .MapResult(RunOptions, HandleParseErrors);
        }

        private static async Task RunOptions(Options opts)
        {
            var host = CreateHostBuilder(Array.Empty<string>()).Build();

            // Set the server password for user auth
            var userService = host.Services.GetRequiredService<IUserService>();
            userService.SetPassword(opts.SharedPassword);

            // Always load the default handlers
            _handlerService = host.Services.GetRequiredService<IHandlerService>();
            _handlerService.LoadDefaultHandlers();

            // If custom Handler
            if (!string.IsNullOrEmpty(opts.HandlerPath))
                await LoadHandler(opts.HandlerPath);

            _serverService = host.Services.GetRequiredService<IServerService>();

            C2Profile profile;
            
            // If custom C2 profile
            if (!string.IsNullOrEmpty(opts.ProfilePath))
                profile = await LoadC2Profile(opts.ProfilePath);
            else // otherwise load the default
                profile = new C2Profile();
            
            _serverService.SetC2Profile(profile);

            await host.RunAsync();
        }

        private static async Task LoadHandler(string path)
        {
            var bytes = await File.ReadAllBytesAsync(path);
            _handlerService.LoadHandler(bytes);
        }

        private static async Task<C2Profile> LoadC2Profile(string path)
        {
            var text = await File.ReadAllTextAsync(path);
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            return deserializer.Deserialize<C2Profile>(text);
        }

        private static async Task HandleParseErrors(IEnumerable<Error> errs)
        {
            foreach (var err in errs)
                await Console.Error.WriteLineAsync(err?.ToString());
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);
            builder.ConfigureWebHostDefaults(Configure);

            return builder;
        }

        private static void Configure(IWebHostBuilder webBuilder)
            => webBuilder.UseStartup<Startup>();
    }
}