using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace LoadBalancerEmulator
{
    public class RxBasedLoadBalancer : LoadBalancerBase
    {
        public RxBasedLoadBalancer(IService[] services, TimeSpan maxResponseTime) : base(services, maxResponseTime)
        {
        }

        public override void Start()
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