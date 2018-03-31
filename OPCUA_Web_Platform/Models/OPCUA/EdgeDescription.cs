using Opc.Ua;

namespace WebPlatform.Models.OPCUA
{
    public class EdgeDescription
    {
        public string PlatformNodeId { get; set; }
        public string DisplayName { get; set; }
        public NodeClass NodeClass { get; set; }
        public NodeId ReferenceTypeId { get; set; }

        public EdgeDescription(string platformNodeId, string displayName, NodeClass nodeClass, NodeId referenceTypeId)
        {
            PlatformNodeId = platformNodeId;
            DisplayName = displayName;
            NodeClass = nodeClass;
            ReferenceTypeId = referenceTypeId;
        }
    }
}