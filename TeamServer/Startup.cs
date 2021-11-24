using System;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using TeamServer.Authentication;
using TeamServer.Hubs;
using TeamServer.Interfaces;
using TeamServer.Services;

namespace TeamServer
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            
            services.AddAuthentication("BasicAuthentication")
                .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "SharpC2", Version = "v1"});
                
                var basicSecurityScheme = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    Reference = new OpenApiReference { Id = "BasicAuth", Type = ReferenceType.SecurityScheme }
                };
                c.AddSecurityDefinition(basicSecurityScheme.Reference.Id, basicSecurityScheme);
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {basicSecurityScheme, Array.Empty<string>()}
                });
            });

            services.AddSignalR();

            // Load services
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IHandlerService, HandlerService>();
            services.AddSingleton<ITaskService, TaskService>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IDroneService, DroneService>();
            services.AddSingleton<IPayloadService, PayloadService>();
            services.AddSingleton<IServerService, ServerService>();
            services.AddSingleton<ICredentialService, CredentialService>();

            services.AddAutoMapper(typeof(Startup));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SharpC2 v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<MessageHub>("/MessageHub");
            });
        }
    }
}