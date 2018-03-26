using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebPlatform.Models.OPCUA
{
    public class MonitorableNode
    {
        public string NodeId { get; set; }
        public int SamplingInterval { get; set; }
        public string DeadBand { get; set; }
        public double DeadBandValue { get; set; }

        //TODO: i parametri numerici possono essere = 0?
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(NodeId) && Regex.IsMatch(NodeId, @"^(\d+)-(?:(\d+)|(\S+))$") &&
                   SamplingInterval > 0 &&
                   DeadBand.Length > 0 && 
                   DeadBandValue > 0;
        }
    }

    public class MonitorParams
    {
        public string Topic { get; set; }
        public string BrokerUrl { get; set; }
        public MonitorableNode[] MonitorableNodes { get; set; }

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(BrokerUrl) && Regex.IsMatch(BrokerUrl, @"^(.*):(.*)$") && 
                   !string.IsNullOrEmpty(Topic) &&
                   MonitorableNodes != null && MonitorableNodes.All(m => m.IsValid());
        }

        public bool IsTelemetryProtocolSupported()
        {
            return Regex.IsMatch(BrokerUrl, @"^(mqtt|signalr):(.*)$");
        }
    }

    public class StopMonitorParams
    {
        public string BrokerUrl { get; set; }
        public string Topic { get; set; }
        
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(BrokerUrl) && Regex.IsMatch(BrokerUrl, @"^(.*):(.*)$") && 
                   !string.IsNullOrEmpty(Topic) && Topic.Length != 0;
        }
    }
}