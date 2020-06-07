using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace DisposableServiceProvider.Tests
{
    /// <summary>
    /// Wraps any inner IServiceProvider and if it is also IDisposable, will ensure Disposal is done in a way that waits for existing scopes to dispose.
    /// </summary>
    public class SafeDisposalServiceProvider<TInnerProvider> : IServiceProvider, IAsyncDisposable, IServiceScopeFactory
        where TInnerProvider : IServiceProvider, IDisposable
    {
        private readonly TInnerProvider innerServiceProvider;
        private CountdownEvent countdownEvent = new CountdownEvent(1);
        private bool _isBeingDisposed = false;

        public SafeDisposalServiceProvider(TInnerProvider innerServiceProvider)
        {
            this.innerServiceProvider = innerServiceProvider;
        }

        public IServiceScope CreateScope()
        {
            if (_isBeingDisposed)
            {
                // prevent new scopes from being created as the aim is to drain down scopes to 0 and then dispose.
                throw new InvalidOperationException("Service provider is being disposed.");
            }

            countdownEvent.AddCount();
            var innerScopeFactory = innerServiceProvider.GetService<IServiceScopeFactory>();
            return new SafeDisposalScope(this, innerScopeFactory.CreateScope(), OnScopeDisposed);

        }

        private void OnScopeDisposed()
        {
            countdownEvent.Signal();
        }

        public async ValueTask DisposeAsync()
        {
            // would be really handy if there as an AsyncCountdownEvent.. :-(
            // spawn a task so we don't block the caller.
            _isBeingDisposed = true;
            await Task.Run(() =>
            {
                countdownEvent.Signal();
                countdownEvent.Wait();
            });

            // should now be safe to dispose
            this.innerServiceProvider.Dispose();
        }

        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceScopeFactory))
            {
                return this;
            }
            return innerServiceProvider.GetService(serviceType);
        }

        internal class SafeDisposalScope : IServiceScope
        {
            private readonly IServiceScope inner;
            private readonly Action onDispose;

            public SafeDisposalScope(IServiceProvider serviceProvider, IServiceScope inner, Action onDispose)
            {
                this.ServiceProvider = serviceProvider;
                this.inner = inner;
                this.onDispose = onDispose;
            }
            public IServiceProvider ServiceProvider { get; }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Simplified for testing purposes")]
            public void Dispose()
            {
                inner.Dispose();
                onDispose();
            }
        }


    }
}
