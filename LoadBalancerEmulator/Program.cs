using System;
using System.Linq;

namespace LoadBalancerEmulator
{
    static class Program
    {
        static void Main()
        {

            var random = new Random();
            var maxResponseTime = TimeSpan.FromSeconds(5);

            Console.WriteLine($"Max response time: {maxResponseTime.TotalMilliseconds} ms");
            Console.WriteLine($"Max Ping time: {maxResponseTime.TotalMilliseconds} ms");
            Console.WriteLine();

            // Each service mock emulates Ping and returns response within a random time < 5 sec
            IService[] services = new[]
                {
                    "a", "b", "c", "d", "e", "f", "g", "h"
                }.Select(name => new ServiceMock(name, random, maxResponseTime))
                .Cast<IService>().ToArray();

            var emulator = 
                //new RxBasedLoadBalancer(services, maxResponseTime);
                new TaskBasedLoadBalancer(services, maxResponseTime);

            emulator.Start();

            Console.ReadLine();
        }
    }
}