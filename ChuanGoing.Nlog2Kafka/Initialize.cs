using NLog.Config;

namespace ChuanGoing.Nlog2Kafka
{
    public static class Initialize
    {
        /// <summary>
        /// 启用KafkaTarget
        /// </summary>
        /// <param name="obj"></param>
        public static void UseKafka()
        {
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("KafkaAsync", typeof(KafkaAsyncTarget));
            ConfigurationItemFactory.Default.Targets.RegisterDefinition("Kafka", typeof(KafkaTarget));
            //LogManager.ReconfigExistingLoggers();
        }
    }
}
