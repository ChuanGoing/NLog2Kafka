using Confluent.Kafka;
using Newtonsoft.Json;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ChuanGoing.Nlog2Kafka
{
    [Target("KafkaAsync")]
    public class KafkaAsyncTarget : AsyncTaskTarget
    {
        // Pooling  
        private readonly ConcurrentQueue<IProducer<Null, string>> _producerPool;
        private int _pCount;
        private int _maxSize;

        // we should caching the instance ip here   
        private ConcurrentDictionary<string, IpModel> _cache;
        private const string IP_CACHE_KEY = "IP_CACHE_KEY";

        public KafkaAsyncTarget()
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

        protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            // Read from cache  
            var instanceIp = GetCurrentIpFromCache();

            // Using RenderLogEvent will allow NLog-Target to make optimal reuse of StringBuilder-buffers.  
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
                await Util.PostMessageAsync(producer, topic, json);
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
