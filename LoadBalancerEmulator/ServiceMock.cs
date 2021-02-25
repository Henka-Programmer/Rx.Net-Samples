using System;
using System.Threading.Tasks;

namespace LoadBalancerEmulator
{
    public interface IService
    {
        Task<bool> Ping();
        string Name { get; }
    }

    public class ServiceMock : IService
    {
        private readonly Random _random;
        private readonly int _maxResponseTime;
        public string Name { get; }


        public ServiceMock(string name, Random random, TimeSpan maxResponseTime)
        {
            _random = random;
            _maxResponseTime = Convert.ToInt32(maxResponseTime.TotalMilliseconds);
            Name = name;
        }

        public async Task<bool> Ping()
        {
            var responseTime = TimeSpan.FromMilliseconds(_random.Next(0, _maxResponseTime));
            await Task.Delay(responseTime);

            Console.WriteLine($"Service [{Name}] responded in {responseTime.TotalMilliseconds}");
            return await Task.FromResult(true);
        }
    }
}