using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadBalancerEmulator
{
    public class TaskBasedLoadBalancer : LoadBalancerBase
    {
        public TaskBasedLoadBalancer(IService[] services, TimeSpan maxResponseTime) : base(services, maxResponseTime)
        {
        }

        public override void Start()
        {
            // Convert IService pings to tasks
            IEnumerable<Task<string>> listenerTasks =
                Services.Select(async service =>
                {
                    await service.Ping();
                    return service.Name;
                });

            var serviceReplyCountWithinTimeout = 0;
            var countCancellationTokenSource = new CancellationTokenSource();
            var countCancellationToken = countCancellationTokenSource.Token;

            var maxTimeoutCancellationTokenSource = new CancellationTokenSource();
            var maxTimeoutCancellationToken = maxTimeoutCancellationTokenSource.Token;

            var respondedServices = new ConcurrentBag<string>();

            var countThresholdConditionTasks = listenerTasks.Select(async task =>
            {
                if (maxTimeoutCancellationToken.IsCancellationRequested || countCancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }

                var serviceName = await task;
                respondedServices.Add(serviceName);
                Interlocked.Increment(ref serviceReplyCountWithinTimeout);
                if (serviceReplyCountWithinTimeout >= 3)
                {
                    // We reach the desired number of services replies within the timeout.
                    countCancellationTokenSource.Cancel();
                }
            });

            // max timeout task, will request cancellation when completed.
            var timeoutConditionTask = Task.Delay(MaxResponseTime / 2).ContinueWith(t => { maxTimeoutCancellationTokenSource.Cancel(); });

            // Counter condition task, observing the counter in a task in order to use it in the Task.WhenAny()
            var counterConditionTask = Task.Factory.StartNew(
                () =>
                //async () =>
            {
                while (!countCancellationToken.IsCancellationRequested) ;// await Task.Delay(10);
                return Task.CompletedTask;
            });

            // exit when all pings completed or the max timeout reached or the the responses count reached.
            Task.WhenAny(Task.WhenAll(countThresholdConditionTasks), timeoutConditionTask, counterConditionTask).ContinueWith(t =>
            {
                var list = string.Join(", ", respondedServices);
                Console.WriteLine($"\t\tResponded in time: {list}");
            }).Wait();
        }
    }
}