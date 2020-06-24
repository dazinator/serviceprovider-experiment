using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScopeTrackingServiceProvider;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sample.Website
{
    /// <summary>
    /// The reason this custom <see cref="IHost"/> exists is simple to expose <see cref="IHost.Services"/>
    /// as our decorated IServiceProvider. The native <see cref="IHost"/> is injected with the default 
    /// ServiceProvider and therefore anything that calls CreateScope() on its <see cref="IHost.Services"/> 
    /// property, will bypass our scope tracking.
    /// </summary>
    internal class DecoratedServiceProviderHost : IHost
    {
        private readonly ILogger<DecoratedServiceProviderHost> _logger;
        private readonly IHostLifetime _hostLifetime;
        private readonly ApplicationLifetime _applicationLifetime;
        private readonly HostOptions _options;
        private IEnumerable<IHostedService> _hostedServices;

        public DecoratedServiceProviderHost(IServiceProvider services,
            IHostApplicationLifetime applicationLifetime,
            ILogger<DecoratedServiceProviderHost> logger,
            IHostLifetime hostLifetime,
            IOptions<HostOptions> options)
        {
            Services = services.DecorateIfDisposable();

            _applicationLifetime = (applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime))) as ApplicationLifetime;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hostLifetime = hostLifetime ?? throw new ArgumentNullException(nameof(hostLifetime));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public IServiceProvider Services { get; }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            //_logger.Starting();

            await _hostLifetime.WaitForStartAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _hostedServices = Services.GetService<IEnumerable<IHostedService>>();

            foreach (var hostedService in _hostedServices)
            {
                // Fire IHostedService.Start
                await hostedService.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            // Fire IApplicationLifetime.Started
            _applicationLifetime?.NotifyStarted();

            // _logger.Started();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            // _logger.Stopping();

            using (var cts = new CancellationTokenSource(_options.ShutdownTimeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
            {
                var token = linkedCts.Token;
                // Trigger IApplicationLifetime.ApplicationStopping
                _applicationLifetime?.StopApplication();

                IList<Exception> exceptions = new List<Exception>();
                if (_hostedServices != null) // Started?
                {
                    foreach (var hostedService in _hostedServices.Reverse())
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            await hostedService.StopAsync(token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                token.ThrowIfCancellationRequested();
                await _hostLifetime.StopAsync(token);

                // Fire IApplicationLifetime.Stopped
                _applicationLifetime?.NotifyStopped();

                if (exceptions.Count > 0)
                {
                    var ex = new AggregateException("One or more hosted services failed to stop.", exceptions);
                    //  _logger.StoppedWithException(ex);
                    throw ex;
                }
            }

            //_logger.Stopped();
        }

        public void Dispose()
        {
            (Services as IDisposable)?.Dispose();
        }
    }

}
