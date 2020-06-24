using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sample.Website
{
    /// <summary>
    /// A HttpContextFactory that includes our <see cref="RequestServicesFeature"/> for tracking request scopes.
    /// </summary>
    public class CustomHttpContextFactory : IHttpContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FormOptions _formOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public CustomHttpContextFactory(IServiceProvider serviceProvider)
        {
            _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            _formOptions = serviceProvider.GetRequiredService<IOptions<FormOptions>>().Value;
            _serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        internal IHttpContextAccessor HttpContextAccessor => _httpContextAccessor;

        public HttpContext Create(IFeatureCollection featureCollection)
        {
            if (featureCollection is null)
            {
                throw new ArgumentNullException(nameof(featureCollection));
            }

            var httpContext = new DefaultHttpContext(featureCollection);
            var feature = new RequestServicesFeature(httpContext, _serviceScopeFactory);
            featureCollection.Set<IServiceProvidersFeature>(feature);

            Initialize(httpContext);
            return httpContext;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Initialize(DefaultHttpContext httpContext, IFeatureCollection featureCollection)
        {
            Debug.Assert(featureCollection != null);
            Debug.Assert(httpContext != null);

            httpContext.Initialize(featureCollection);

            Initialize(httpContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private DefaultHttpContext Initialize(DefaultHttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = httpContext;
            }

            httpContext.FormOptions = _formOptions;
            httpContext.ServiceScopeFactory = _serviceScopeFactory;

            return httpContext;
        }

        public void Dispose(HttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = null;
            }
        }

        internal void Dispose(DefaultHttpContext httpContext)
        {
            if (_httpContextAccessor != null)
            {
                _httpContextAccessor.HttpContext = null;
            }

            httpContext.Uninitialize();
        }
    }

}
