using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace DisposableServiceProvider.Tests
{
    public class ServiceProvider_Dispose
    {
        [Fact]
        public void Dispose_Breaks_SingletonIDisposables_In_OtherScopes ()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DisposableService>();
            var rootServiceProvider = services.BuildServiceProvider();

            using var scope = rootServiceProvider.CreateScope();
            var singleton = scope.ServiceProvider.GetRequiredService<DisposableService>();
            singleton.DoSomething(); // all good.

            rootServiceProvider.Dispose(); // now all bad.
            Assert.Throws<ObjectDisposedException>(() => singleton.DoSomething());
        }
    }    


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Simplified for test purposes")]
    public class DisposableService : IDisposable
    {
        public bool WasDisposed { get; set; } = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Simplified for test purposes")]
        public void Dispose()
        {
            WasDisposed = true;
        }

        public void DoSomething()
        {
            if (WasDisposed)
            {
                throw new ObjectDisposedException(nameof(DisposableService));
            }
        }
    }
}
