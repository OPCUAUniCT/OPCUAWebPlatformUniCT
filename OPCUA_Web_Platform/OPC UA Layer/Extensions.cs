using Opc.Ua;

namespace WebPlatform.Extensions
{
    public static class ExpandedNdeIdExtension
    {
        public static NodeId ToNodeId(this ExpandedNodeId expandedNodeId)
        {
            var temp = new NodeId(expandedNodeId.Identifier, expandedNodeId.NamespaceIndex);
            return temp;

        }
        
        public static string ToStringId(this ExpandedNodeId expandedNodeId)
        {
            return $"{expandedNodeId.NamespaceIndex}-{expandedNodeId.Identifier}";
        }
    }
}