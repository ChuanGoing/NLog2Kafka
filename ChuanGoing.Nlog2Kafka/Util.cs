using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Threading.Tasks;

namespace ChuanGoing.Nlog2Kafka
{
    public class Util
    {
        public static IProducer<Null, string> NewProducer(string bootstrapServers,
            int? queueBufferingMaxMessages,
            int? retryBackoffMs,
            int? messageSendMaxRetries)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                QueueBufferingMaxMessages = queueBufferingMaxMessages,
                RetryBackoffMs = retryBackoffMs,
                MessageSendMaxRetries = messageSendMaxRetries
            };

            var producer = new ProducerBuilder<Null, string>(config).Build();

            return producer;
        }

        public static DeliveryResult<Null, string> PostMessage(IProducer<Null, string> producer, string topic, string message)
        {
            return producer.ProduceAsync(topic, new Message<Null, string>()
            {
                Value = message
            }).GetAwaiter().GetResult();
        }

        public static async Task PostMessageAsync(IProducer<Null, string> producer, string topic, string message)
        {
            await producer.ProduceAsync(topic, new Message<Null, string>()
            {
                Value = message
            });
        }

        public static string GetCurrentIp()
        {
            var instanceIp = "127.0.0.1";

            try
            {
                IPHostEntry ipHost = Dns.GetHostEntry(Dns.GetHostName());

                foreach (var ipAddr in Dns.GetHostAddresses(Dns.GetHostName()))
                {
                    if (ipAddr.AddressFamily.ToString() == "InterNetwork")
                    {
                        instanceIp = ipAddr.ToString();
                        break;
                    }
                }
            }
            catch
            {
            }

            return instanceIp;
        }

        public static string JsonSerialize(string level, string appname, string instanceIp, string traceId, string requestIp,
            string logname, string msg)
        {
            return JsonConvert.SerializeObject(new
            {
                dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                level,
                appname,
                instanceIp,
                traceId,
                requestIp,
                @class = logname,
                message = msg
            });
        }
    }
}
