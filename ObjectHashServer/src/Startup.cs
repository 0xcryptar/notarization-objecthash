using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ObjectHashServer.BLL.Utils;

namespace ObjectHashServer
{
    public class Startup
    {
        private readonly IConfiguration  _configuration;
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            if (string.Equals(_configuration["SSL_STATUS"], "Enabled", StringComparison.CurrentCultureIgnoreCase))
            {
                // Add SSL encryption using Lets Encrypt
                services.AddLettuceEncrypt();
            }
            
            services.AddMvc(config =>
            {
                config.Filters.Add(new ExceptionHandlerFilterAttribute());
            }).SetCompatibilityVersion(CompatibilityVersion.Version_3_0).AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            app.UseHsts();
            app.UseHttpsRedirection();

            // matches request to an endpoint
            app.UseRouting();
            
            app.UseCors(builder =>
                builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithOrigins("https://www.trueprofile.io")
                    .WithOrigins("https://dev.trueprofile.io")
                    .WithOrigins("https://stage.trueprofile.io")
                    .WithOrigins("https://member.dev.trueprofile.io")
                    .WithOrigins("https://member.stage.trueprofile.io")
                    .WithOrigins("https://member.trueprofile.io")
                    .WithOrigins("https://partner.dev.trueprofile.io")
                    .WithOrigins("https://partner.stage.trueprofile.io")
                    .WithOrigins("https://partner.trueprofile.io")
            );
            
            if (_environment.IsDevelopment() || _environment.IsStaging())
            {
                app.UseDeveloperExceptionPage();
            }

            // place UseAuthentication and UseAuthorization after UseRouting and UseCors but before UseEndpoints
            app.UseAuthentication();

            // execute the matched endpoint
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
