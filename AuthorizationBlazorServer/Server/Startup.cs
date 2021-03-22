using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;
using System.Reflection;
using IdentityServer4.EntityFramework.Entities;
using System.Collections.Generic;
using IdentityServerApp.Identity;
using IdentityServer4.EntityFramework.Mappers;
using AuthorizationBlazorServer.Server.Services;
using Microsoft.AspNetCore.Authentication.Google;
using IdentityServer4;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AuthorizationBlazorServer.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
           
            services.AddControllersWithViews();
            services.AddRazorPages();


            services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
            )
            .AddJwtBearer(options =>
            {
              options.Authority = Configuration["AuthorizationServer:Authority"];
              options.Audience = Configuration["AuthorizationServer:Audience"];
            });

            string ConnectionString = Configuration.GetConnectionString("IdentityServerDb");
            string UserConnectionString = Configuration.GetConnectionString("UserDb");
            string MigrationsAssembly = Assembly.GetExecutingAssembly().GetName().Name;
            var Builder = services.AddIdentityServer()
                .AddUsers(options =>
                {
                    options.UseSqlServer(UserConnectionString);
                })
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(ConnectionString, options => 
                    options.MigrationsAssembly(MigrationsAssembly));
                })
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                    builder.UseSqlServer(ConnectionString, options =>
                    options.MigrationsAssembly(MigrationsAssembly));
                })
                .AddDeveloperSigningCredential();
            services.AddAuthentication().AddGoogle(GoogleDefaults.AuthenticationScheme, options
                 =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = "";
                options.ClientSecret = "";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseIdentityServer();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
