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

        public override async Task StartAsync()
        {
            Console.WriteLine("# Task Based Load Balancer");
            Console.WriteLine();

            // Infinity repeat with delay
            while (true)
            {
                await CheckServices();
                await Task.Delay(MaxResponseTime);
                Console.WriteLine();
                Console.WriteLine();
            }
        }

        
        private async Task CheckServices()
        {
            // Convert IService pings to tasks
            IEnumerable<Task<string>> listenerTasks =
                Services.Select(async service =>
                {
                    await service.Ping();
                    return service.Name;
                });

            var serviceReplyCountWithinTimeout = 0;

            var maxTimeoutCancellationTokenSource = new CancellationTokenSource();
            var maxTimeoutCancellationToken = maxTimeoutCancellationTokenSource.Token;

            var respondedServices = new ConcurrentBag<string>();

            var countThresholdConditionTasks = listenerTasks.Select(async task =>
            {
                if (maxTimeoutCancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var serviceName = await task;

                respondedServices.Add(serviceName);
                Interlocked.Increment(ref serviceReplyCountWithinTimeout);

                //// this will allow us to report only the first three services
                //if (serviceReplyCountWithinTimeout == 3)
                //{
                //    // We reach the desired number of services replies within the timeout. 
                //    var list = string.Join(", ", respondedServices);
                //    Console.WriteLine($"\t\tResponded in time: {list}");
                //}
            });

            // max timeout task, will request cancellation when completed.
            var timeoutConditionTask = Task
                .Delay(PingTimeout, maxTimeoutCancellationToken)
                .ContinueWith(_ =>
                {
                    maxTimeoutCancellationTokenSource.Cancel();

                    // the services replies within the timeout. 
                    var list = string.Join(", ", respondedServices);
                    Console.WriteLine($"\t\tResponded in time: {list}");

                }, maxTimeoutCancellationToken);

            // exit when all pings completed or the max timeout reached or the the responses count reached.
            await Task.WhenAny(Task.WhenAll(countThresholdConditionTasks), timeoutConditionTask);
        }
    }
}