namespace ChuanGoing.Nlog2Kafka
{
    using System;

    internal class IpModel
    {
        public string Ip { get; set; }

        public DateTimeOffset Expiration { get; set; }
    }
}
