﻿using Autofac;
using Hangfire;
using Hangfire.AspNetCore.Multitenant;
using Hangfire.AspNetCore.Multitenant.Request;
using Hangfire.Initialization;
using Hangfire.States;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MultiTenantDashboard
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .AddControllersAsServices()
                .AddHangfireTenantViewLocations();

            //Tenant Store
            //services.AddHangfireMultiTenantStore<HangfireTenantsInMemory>();
            services.AddHangfireMultiTenantStore<HangfireTenantsJson>();

            //Tenant Setup
            services.AddHangfireMultiTenantSetup<HangfireTenantSetup>();

            //Tenant Configuration
            services.AddHangfireTenantConfiguration();

            //Tenant Identification
            services.AddHangfireTenantRequestIdentification().TenantFromHostQueryStringSourceIP();

            //404 if no tenant identified
            services.AddHangfireTenant404Middleware();


            // -- Hangfire Setup --

            //Default dashboard options.
            services.AddSingleton(sp => new DashboardOptions()
            {
                Authorization = new[] { new HangfireRoleAuthorizationfilter("admin") }
            });

            //Default filter options.
            services.AddHangfire(config => {
                config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

                config.UseFilter(new HangfireLoggerAttribute());
                config.UseFilter(new HangfirePreserveOriginalQueueAttribute());
            });

            //Default Hangfire Server options
            services.AddSingleton(sp => new BackgroundJobServerOptions()
            {
                ServerName ="web-background",
                WorkerCount = 1 //Default  Math.Min(Environment.ProcessorCount * 5, 20);
            });
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {

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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseHangfireDashboardMultiTenant("/hangfire");

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
