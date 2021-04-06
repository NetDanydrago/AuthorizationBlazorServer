using IdentityServer4.EntityFramework.DbContexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using AuthorizationBlazorServer.Server.Services;
using Microsoft.AspNetCore.Authentication.Google;
using IdentityServer4;
using AuthorizationBlazorServer.Server.Policies;

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
                options.ClientId = "808883587000-ce6a2fnuj2r24slgcjmli1m4mg5885j9.apps.googleusercontent.com";
                options.ClientSecret = "j8Giz_CVRQUHgn9xKcbEuW1B";
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy(RolePolicies.SAdmin,
                    RolePolicies.SAdminPolicy());
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
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
