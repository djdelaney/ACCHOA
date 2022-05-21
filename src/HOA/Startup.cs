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
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

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
            services.AddControllersWithViews();

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
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();


            app.UseStatusCodePages();

            app.UseResponseCompression();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
