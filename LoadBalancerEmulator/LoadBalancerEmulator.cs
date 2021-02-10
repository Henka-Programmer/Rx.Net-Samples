using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Rx.Samples
{

    public class LoadBalancerEmulator
    {
        public TimeSpan MaxResponseTime { get; private set; } = TimeSpan.FromSeconds(5);
        public TimeSpan PingTimeout { get => MaxResponseTime / 2; }
        public IService[] Services { get; private set; }
        public LoadBalancerEmulator(IService[] services, TimeSpan maxResponseTime)
        {
            Services = services;
            MaxResponseTime = maxResponseTime;  
        }

        public void EmulateUsingTasks()
        {
            // Convert IService pings to tasks
            IEnumerable<Task<string>> serverObservables =
                Services.Select(async service =>
               {
                   await service.Ping();
                   return service.Name;
               });

            var tasksRepyesWithinTimeoutCount = 0;
            var countCancellationTokenSource = new CancellationTokenSource();
            var countCancellationToken = countCancellationTokenSource.Token;

            var maxTimeoutCancellationTokenSource = new CancellationTokenSource();
            var maxTimeoutCancellationToken = maxTimeoutCancellationTokenSource.Token;

            ConcurrentBag<string> respondedServices = new ConcurrentBag<string>();

            var tasks = serverObservables.Select(async task =>
            {
                if (maxTimeoutCancellationToken.IsCancellationRequested || countCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var serviceName = await task;
                respondedServices.Add(serviceName);
                Interlocked.Increment(ref tasksRepyesWithinTimeoutCount);
                if (tasksRepyesWithinTimeoutCount >= 3)
                {
                    // We reach the desired number of services replyes within the timeout.
                    countCancellationTokenSource.Cancel();
                }
            });

            // max timeout task, will request cancellation when completed.
            var maxTimeOutTask = Task.Delay(MaxResponseTime / 2).ContinueWith(t => { maxTimeoutCancellationTokenSource.Cancel(); });

            // Counter condition task, observing the counter in a task in order to use it in the Task.WhenAny()
            var counterTask = Task.Factory.StartNew(() =>
            {
                while (!countCancellationToken.IsCancellationRequested) ;
                return Task.CompletedTask;
            });

            // exit when all pings completed or the max timeout reached or the the responses count reached.
            Task.WhenAny(Task.WhenAll(tasks), maxTimeOutTask, counterTask).ContinueWith(t =>
            {
                var list = string.Join(", ", respondedServices);
                Console.WriteLine($"\t\tResponded in time: {list}");
            }).Wait();
        }

        public void EmulateUsingRx()
        {
            // Convert IService pings to observables
            IEnumerable<IObservable<string>> serverObservables =
                Services.Select(async service =>
                {
                    await service.Ping();
                    return service.Name;
                }).Select(task => task.ToObservable());

            // Merge the Observables into a single Stream to observe it as a whole cluster
            IObservable<string> clusterObservable = serverObservables.Merge();

            IObservable<IList<string>> observationPolicy = clusterObservable
                .Buffer(PingTimeout) //buffering the items that arrived within a timeout
                .Take(1); // look at the services that responded before a timeout

            // and repeat the observation with a delay of 2 seconds
            IObservable<IList<string>> delay = Observable.Empty<IList<string>>().Delay(TimeSpan.FromSeconds(1));

            observationPolicy = observationPolicy
                .Concat(delay) // concat the delay observable
                .Concat(Observable.FromAsync(() =>
                {
                    return Task.Factory.StartNew(() =>
                    {
                        Console.WriteLine();
                        return new List<string>();
                    });
                })) // concat the divider task between repeat iterations to print the dividre in the console.
                .Repeat();

            observationPolicy.Subscribe(respondedServices =>
            {
                var list = string.Join(", ", respondedServices);
                Console.WriteLine($"\t\tResponded in time: {list}");
            });
        }
    }
}
