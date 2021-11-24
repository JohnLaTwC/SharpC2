using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CommandLine;

using Microsoft.Extensions.DependencyInjection;

using SharpC2.Services;

namespace SharpC2
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            PrintLogo();

            await Parser.Default.ParseArguments<Options>(args)
                .MapResult(RunOptions, HandleParseErrors);
        }

        private static async Task RunOptions(Options opts)
        {
            var sp = BuildServiceProvider();

            var apiService = sp.GetRequiredService<ApiService>();
            apiService.Server = opts.Server;
            apiService.Port = opts.Port;
            apiService.Nick = opts.Nick;
            apiService.Password = opts.Password;
            apiService.IgnoreSsl = opts.IgnoreSsl;
            apiService.StartClient();

            var signalRService = sp.GetRequiredService<SignalRService>();
            signalRService.Server = opts.Server;
            signalRService.Port = opts.Port;
            signalRService.Nick = opts.Nick;
            signalRService.Password = opts.Password;
            await signalRService.Connect();

            var screenService = sp.GetRequiredService<ScreenService>();
            var dashboard = screenService.GetScreen(ScreenService.ScreenType.Drones);

            Console.WriteLine();
            await dashboard.Show();
        }

        private static void PrintLogo()
        {
            Console.WriteLine(@"  ___ _                   ___ ___ ");
            Console.WriteLine(@" / __| |_  __ _ _ _ _ __ / __|_  )");
            Console.WriteLine(@" \__ \ ' \/ _` | '_| '_ \ (__ / / ");
            Console.WriteLine(@" |___/_||_\__,_|_| | .__/\___/___|");
            Console.WriteLine(@"                   |_|            ");
            Console.WriteLine(@"    @_RastaMouse                  ");
            Console.WriteLine(@"    @_xpn_                        ");
            Console.WriteLine();
        }
        
        private static ServiceProvider BuildServiceProvider()
        {
            var serviceCollection = new ServiceCollection()
                .AddSingleton<SslService>()
                .AddSingleton<ApiService>()
                .AddSingleton<SignalRService>()
                .AddSingleton<ScreenService>()
                .AddAutoMapper(typeof(Program));

            return serviceCollection.BuildServiceProvider();
        }

        private static async Task HandleParseErrors(IEnumerable<Error> errs)
        {
            foreach (var err in errs)
                await Console.Error.WriteLineAsync(err?.ToString());
        }
    }
}