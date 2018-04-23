using Opc.Ua.Client;
using WebPlatform.Monitoring;

namespace WebPlatform.Models.OPCUA
{
    public class MonitorPublishInfo
    {
        public string Topic { get; set; }
        public string BrokerUrl { get; set; }
        public Subscription Subscription { get; set; }
        public IPublisher Publisher { get; set; }
        
        public void Forward(string message)
        {
            Publisher.Publish(Topic, message);
        }
    }
}