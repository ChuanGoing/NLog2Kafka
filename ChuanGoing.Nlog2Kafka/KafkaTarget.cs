using Confluent.Kafka;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ChuanGoing.Nlog2Kafka
{
    [Target("Kafka")]
    public class KafkaTarget : TargetWithLayout
    {
        private readonly ConcurrentQueue<IProducer<Null, string>> _producerPool;
        private int _pCount;
        private int _maxSize;
        private ConcurrentDictionary<string, IpModel> _cache;

        private const string IP_CACHE_KEY = "IP_CACHE_KEY";

        public KafkaTarget()
        {
            _producerPool = new ConcurrentQueue<IProducer<Null, string>>();
            _maxSize = 10;
            _cache = new ConcurrentDictionary<string, IpModel>();
        }

        [RequiredParameter]
        public Layout Topic { get; set; }

        [RequiredParameter]
        public Layout AppName { get; set; }

        [RequiredParameter]
        public Layout TraceId { get; set; }

        [RequiredParameter]
        public Layout RequestIp { get; set; }


        [RequiredParameter]
        public string BootstrapServers { get; set; }

        public int? QueueBufferingMaxMessages { get; set; }
        public int? RetryBackoffMs { get; set; }
        public int? MessageSendMaxRetries { get; set; }

        protected override void CloseTarget()
        {
            base.CloseTarget();
            _maxSize = 0;
            while (_producerPool.TryDequeue(out var context))
            {
                context.Dispose();
            }
        }

        private IProducer<Null, string> RentProducer()
        {
            if (_producerPool.TryDequeue(out var producer))
            {
                Interlocked.Decrement(ref _pCount);

                return producer;
            }

            return Util.NewProducer(BootstrapServers, QueueBufferingMaxMessages,
                RetryBackoffMs, MessageSendMaxRetries);
        }

        private bool Return(IProducer<Null, string> producer)
        {
            if (Interlocked.Increment(ref _pCount) <= _maxSize)
            {
                _producerPool.Enqueue(producer);

                return true;
            }

            Interlocked.Decrement(ref _pCount);

            return false;
        }

        private string GetCurrentIpFromCache()
        {
            if (_cache.TryGetValue(IP_CACHE_KEY, out var obj))
            {
                return DateTimeOffset.UtcNow.Subtract(obj.Expiration) < TimeSpan.Zero
                    ? obj.Ip
                    : BuildCacheAndReturnIp();
            }
            else
            {
                return BuildCacheAndReturnIp();
            }
        }

        private string BuildCacheAndReturnIp()
        {
            var newObj = new IpModel
            {
                Ip = Util.GetCurrentIp(),
                Expiration = DateTimeOffset.UtcNow.AddMinutes(5),
            };

            _cache.AddOrUpdate(IP_CACHE_KEY, newObj, (x, y) => newObj);

            return newObj.Ip;
        }



        protected override void Write(LogEventInfo logEvent)
        {
            var instanceIp = GetCurrentIpFromCache();

            string topic = RenderLogEvent(Topic, logEvent);
            string appname = RenderLogEvent(AppName, logEvent);
            string traceId = RenderLogEvent(TraceId, logEvent);
            string requestIp = RenderLogEvent(RequestIp, logEvent);
            string msg = RenderLogEvent(Layout, logEvent);

            var json = Util.JsonSerialize(logEvent.Level.Name.ToUpper(), appname, instanceIp, traceId, requestIp,
                logEvent.LoggerName, msg);

            var producer = RentProducer();

            try
            {
                var result = Util.PostMessage(producer, topic, json);
            }
            catch (Exception ex)
            {
                InternalLogger.Error(ex, $"kafka published error.");
            }
            finally
            {
                var returned = Return(producer);
                if (!returned)
                {
                    producer.Dispose();
                }
            }

            base.Write(logEvent);
        }
    }
}