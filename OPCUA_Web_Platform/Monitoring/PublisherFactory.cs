using System;
using WebPlatform.Monitoring.MQTT;

namespace WebPlatform.Monitoring
{
    public static class PublisherFactory
    {
        public static IPublisher GetPublisherForProtocol(string protocol, string url)
        {
            switch (protocol)
            {
                case "mqtt":
                    return new MqttPublisher(url);
                default:
                    throw new Exception($"A publisher for the technology {protocol} does not exist.");
            }
        }
    }
}