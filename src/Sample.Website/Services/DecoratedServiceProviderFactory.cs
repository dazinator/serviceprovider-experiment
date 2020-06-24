using System;
using ScopeTrackingServiceProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Sample.Website
{
    public class DecoratedServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            var inner = containerBuilder.BuildServiceProvider();
            return inner.DecorateIfDisposable();
        }
    }

}
