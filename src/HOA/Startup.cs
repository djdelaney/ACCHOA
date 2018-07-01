using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HOA.Model;
using Microsoft.EntityFrameworkCore;
using HOA.Services;
using HOA.Util;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HOA
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights 
                // pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
                builder.AddUserSecrets("aspnet-HOANEW-b9a2d01d-ce96-40a1-8b4a-fc668192a400");
            }
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add Entity Framework services to the services container.
            services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Configuration["SqlConnectionString"]));

            // Add Identity services to the services container.
            services.AddIdentity<ApplicationUser, IdentityRole>
                (
                    options =>
                    {
                        options.Password.RequireDigit = false;
                        options.Password.RequireLowercase = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequiredLength = 6;
                    }
                )
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddResponseCompression();

            // Add MVC services to the services container.
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            //Storage
            var azureCon = Configuration["AzureStorageConnectionString"];
            if (string.IsNullOrEmpty(azureCon))
                services.AddTransient<IFileStore, MockFileStore>();
            else
            {
                AzureFileStore.ConnectionString = azureCon;
                services.AddTransient<IFileStore, AzureFileStore>();
            }

            //Email
            EmailHelper.BaseHost = Configuration["EmailLinkHost"];
            var sendGridKey = Configuration["EmailKey"];
            var emailSource = Configuration["EmailSource"];
            if (string.IsNullOrEmpty(sendGridKey) || string.IsNullOrEmpty(emailSource))
                services.AddTransient<IEmailSender, MockEmail>();
            else
            {
                SendGridEmail.ApiKey = sendGridKey;
                SendGridEmail.EmailSource = emailSource;
                services.AddTransient<IEmailSender, SendGridEmail>();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            
            app.UseStatusCodePages();

            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            // Add static files to the request pipeline.
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // Add cookie-based authentication to the request pipeline.
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
