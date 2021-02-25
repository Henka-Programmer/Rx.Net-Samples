using System;

namespace LoadBalancerEmulator
{
    public abstract class LoadBalancerBase
    {
        public TimeSpan MaxResponseTime { get; }
        public TimeSpan PingTimeout => MaxResponseTime / 2;
        public IService[] Services { get; }
        public LoadBalancerBase(IService[] services, TimeSpan maxResponseTime)
        {
            Services = services;
            MaxResponseTime = maxResponseTime;
        }

        public abstract void Start();
    }
}
