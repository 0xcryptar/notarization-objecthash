using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ObjectHashServer.Utils;

namespace ObjectHashServer
{
    public class Startup
    {
        private readonly IConfiguration  _configuration;
        
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
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
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Hsts will be configure later
                // app.UseHsts();
            }

            // app.UseMiddleware(typeof(ExceptionHandlingMiddleware));

            // No redirect to https, https will be configure from devops
            // app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
