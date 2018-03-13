using System.Text;
using uPLibrary.Networking.M2Mqtt;

namespace WebPlatform.Monitoring.MQTT
{
    interface IMqttPublisher : IPublisher {}
    public class MqttPublisher: IMqttPublisher
    {
        private readonly MqttClient _mClient;
        
        public MqttPublisher(string mqtturl)
        {
            _mClient = new MqttClient(mqtturl);
            //TODO Check authentication because whoever knows "OPC-WebApi" string can connect to the broker
            _mClient.Connect("OPC-WebApi");
        }
        
        public void Publish(string topic, string message)
        {
            _mClient.Publish(topic, Encoding.UTF8.GetBytes(message));
        }
    }
}