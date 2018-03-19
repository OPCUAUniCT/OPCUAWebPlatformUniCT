using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt;

namespace WebPlatform.Monitoring.MQTT
{
    interface IMqttPublisher : IPublisher {}
    public class MqttPublisher: IMqttPublisher
    {
        private readonly MqttClient _mClient;
        private static readonly Dictionary<string, MqttClient> ClientsDict = new Dictionary<string, MqttClient>();
        
        public MqttPublisher(string mqtturl)
        {
            if (ClientsDict.ContainsKey(mqtturl))
            {
                _mClient = ClientsDict[mqtturl];
            }
            else
            {
                _mClient = new MqttClient(mqtturl);
                _mClient.Connect("OPC-WebApi");
                ClientsDict.Add(mqtturl, _mClient);
            }
        }
        
        public void Publish(string topic, string message)
        {
            _mClient.Publish(topic, Encoding.UTF8.GetBytes(message));
        }
    }
}