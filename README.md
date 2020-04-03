# NLog2Kafka
Git source code address:https://github.com/ChuanGoing/NLog2Kafka.git

This is a toolkit for logging Nlog to kafka. Note: Before starting to use, please call Initialize.UseKafka() for initialization. Otherwise, it will prompt that 'KafkaTarget' cannot be found. 
这是一个NLog日志写入kafka的工具包，使用之前注意调用Initialize.UseKafka()进行初始化，否则将会提示找不到"KafkaTarget"类型。

NLog.config like this demo:
<?xml version="1.0" encoding="utf-8" ?>
<nlog autoReload="true" xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
  throwExceptions="true" throwConfigExceptions="true" internalLogLevel="Trace" >
  <targets>
    <target xsi:type ="Kafka" name="kafka" topic="topic" appname="TestSvr"
        traceId ="${aspnet-request:item=traceId}"
        requestIp = "${aspnet-request:item=requestIp}"
        bootstrapServers = "127.0.0.1:9092"
        MessageSendMaxRetries="3"
        layout="${message}">
    </target>
  </targets>

  <rules>
    <logger name="*" minlevel="Debug" writeTo="kafka" />
  </rules>
</nlog>

