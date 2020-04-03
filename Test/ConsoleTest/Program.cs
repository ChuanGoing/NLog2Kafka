using ChuanGoing.Nlog2Kafka;
using NLog.Web;
using System;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Initialize.UseKafka();

            var logger = NLogBuilder.ConfigureNLog($"Nlog.config").GetCurrentClassLogger();
            bool isCon = true;
            while (isCon)
            {
                isCon = false;
                Console.WriteLine("按I/W 发送消息...");
                var r = Console.ReadLine();
                string n = Guid.NewGuid().ToString();
                switch (r.ToUpper())
                {
                    case "I":
                        isCon = true;
                        logger.Info($"Info AAAAAA {n}");
                        break;
                    case "W":
                        isCon = true;
                        logger.Warn($"Warn BBBBBB {n}");
                        break;
                    default:
                        isCon = false;
                        break;
                }
            }
            Console.WriteLine("按任意键推出...");
            Console.ReadKey();

        }
    }
}
