using System.Linq;
using ScopeTrackingServiceProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sample.Website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
            .ConfigureServices((a) =>
            {
                // We need a custom HttpContextFactory because we need to initilise HttpContext with a custom
                // ServicesProvider IFeature
                a.AddSingleton<IHttpContextFactory, CustomHttpContextFactory>(sp => new CustomHttpContextFactory(sp.DecorateIfDisposable()));

                // We need a custom IHost because the default IHost.ApplicationServices is not set to our decorated IServiceProvider that tracks scopes.
                a.AddSingleton<IHost, DecoratedServiceProviderHost>();

                // We need to create the ApplicationBuilderFactory ourselves so we 
                // can pass it our decorated IServiceProvider instance that will track scopes.
                a.AddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>(sp => new ApplicationBuilderFactory(sp.DecorateIfDisposable()));

            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .UseServiceProviderFactory(new DecoratedServiceProviderFactory());
    }
}
