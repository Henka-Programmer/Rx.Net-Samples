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
        public override Task StartAsync()
        {
            Console.WriteLine("# Rx Based Load Balancer");
            Console.WriteLine();

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
                    .Take(1)
                    .Do(respondedServices =>
                    {
                        var list = string.Join(", ", respondedServices);
                        Console.WriteLine($"\t\tResponded in time: {list}");
                    }); // look at the services that responded before a timeout


            // and repeat the observation with a delay of 2 seconds
            IObservable<IList<string>> delay = Observable.Empty<IList<string>>().Delay(MaxResponseTime);

            observationPolicy = observationPolicy
                .Concat(delay) // concat the delay observable
                .Do(onNext => { }, () =>
                 {
                     Console.WriteLine();
                     Console.WriteLine();
                 })
                .Repeat();

            // as the observables are lazy, so won't start until we subscribed
            // we can do an empty subscription in order to invoke the observables to run.
            // or just return the observable as a task 
            return observationPolicy.ToTask();
        }
    }
}