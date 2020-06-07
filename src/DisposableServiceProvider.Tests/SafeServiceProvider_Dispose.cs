using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace DisposableServiceProvider.Tests
{
    public class SafeServiceProvider_Dispose
    {
        [Fact]
        public async Task AsyncDispose_AwaitingInsideScope_Deadlocks()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DisposableService>();

            var rootServiceProvider = new SafeDisposalServiceProvider<ServiceProvider>(services.BuildServiceProvider());

            using var scope = rootServiceProvider.CreateScope();
            var singleton = scope.ServiceProvider.GetRequiredService<DisposableService>();
            singleton.DoSomething(); // all good.

            // Calling DisposeAsync() will cause a deadlock 
            // because it waits for all scopes to dispose before completing,
            // but we are holding onto a scope here.
            var deadlockedTask = rootServiceProvider.DisposeAsync();
            int waitIterations = 5;
            while (!deadlockedTask.IsCompleted && waitIterations > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                waitIterations = waitIterations - 1;
            }

            Assert.False(deadlockedTask.IsCompleted); // has deadlocked!
        }

        [Fact]
        public async Task AsyncDispose_AwaitingOutsideScope_Succeeds()
        {
            var services = new ServiceCollection();
            services.AddSingleton<DisposableService>();

            var rootServiceProvider = new SafeDisposalServiceProvider<ServiceProvider>(services.BuildServiceProvider());

            var rand = new Random();
            async Task Worker()
            {
                using (var scope = rootServiceProvider.CreateScope())
                {
                    // simulate some arbitrary work
                    await Task.Delay(TimeSpan.FromSeconds(rand.Next(1, 10)));
                    var singleton = scope.ServiceProvider.GetRequiredService<DisposableService>();
                    singleton.DoSomething(); // never broken.
                }
            }
           
            List<Task> workerTasks = new List<Task>();          
            for (int i = 0; i < 10; i++)
            {                
                workerTasks.Add(Worker());
            }
            var allWorkerTasks = Task.WhenAll(workerTasks);

            var singleton = rootServiceProvider.GetRequiredService<DisposableService>();
            await rootServiceProvider.DisposeAsync();

            Assert.Throws<ObjectDisposedException>(() => singleton.DoSomething());
            await allWorkerTasks; 
           
        }
    }
}
