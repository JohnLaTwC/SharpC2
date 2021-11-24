using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using TeamServer.Models;

namespace TeamServer.Handlers
{
    public class DefaultHttpHandler : Handler
    {
        public sealed override string Name { get; } = "default-http";
        
        private readonly string _workingDirectory;

        public DefaultHttpHandler()
        {
            _workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Handlers", Name);
            if (!Directory.Exists(_workingDirectory)) Directory.CreateDirectory(_workingDirectory);
        }

        public override List<HandlerParameter> Parameters { get; } = new()
        {
            new HandlerParameter("BindPort", "80", false),
            new HandlerParameter("ConnectAddress", "localhost", false),
            new HandlerParameter("ConnectPort", "80", false)
        };

        public override Task Start()
        {
            // this throws if the handler doesn't have the required parameters set
            base.Start();

            var host = new HostBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseUrls($"http://0.0.0.0:{GetParameter("BindPort")}");
                    webBuilder.Configure(ConfigureApp);
                    webBuilder.ConfigureServices(ConfigureServices);
                    webBuilder.ConfigureKestrel(ConfigureKestrel);
                })
                .Build();

            return host.RunAsync(TokenSource.Token);
        }

        private static void ConfigureKestrel(KestrelServerOptions k)
        {
            k.AddServerHeader = false;
        }

        private void ConfigureApp(IApplicationBuilder app)
        {
            app.UseRouting();
            app.UseEndpoints(e =>
            {
                e.MapControllerRoute("/", "/", new
                {
                    controller = "DefaultHttpHandler",
                    action = "RouteDrone"
                });
            });
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(_workingDirectory),
                ServeUnknownFileTypes = true
            });
            
            Debug.WriteLine(_workingDirectory);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton(MessageHub);
            services.AddSingleton(TaskService);
            services.AddSingleton(CredentialService);
        }

        public async Task AddHostedFile(byte[] content, string filename)
        {
            var path = Path.Combine(_workingDirectory, filename);
            await File.WriteAllBytesAsync(path, content);
        }

        public IEnumerable<FileInfo> GetHostedFiles()
        {
            var files = Directory.EnumerateFiles(_workingDirectory);
            return files.Select(file => new FileInfo(file)).ToArray();
        }

        public bool RemoveHostedFile(string filename)
        {
            var path = Path.Combine(_workingDirectory, filename);
            if (!File.Exists(path)) return false;
            
            File.Delete(path);
            return true;
        }

        public override void Stop()
        {
            TokenSource.Cancel();
            TokenSource.Dispose();
        }
    }
}