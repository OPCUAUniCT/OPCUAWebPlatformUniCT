namespace WebPlatform.Models.OPCUA
{
    public class OPCElementToMonitor
    {
        public string NodeId { get; set; }
        public int SamplingInterval { get; set; }
        public string DeadBand { get; set; }
        public double DeadBandValue { get; set; }

    }
}