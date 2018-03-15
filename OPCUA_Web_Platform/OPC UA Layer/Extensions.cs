using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Text.RegularExpressions;

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

        public static int[] GetDimensions(this JArray jArray)
        {
            bool isLast = false;
            JToken jToken = jArray;
            while (!isLast)
            {
                try
                {
                    jToken = jToken.Last;
                }
                catch
                {
                    isLast = true;
                }
            }

            String pattern = @"\[(\d+)\]";
            Regex regex = new Regex(pattern);
            MatchCollection matchColl = regex.Matches(jToken.Path);
            int[] dimensions = new int[matchColl.Count];
            for (int i = 0; i < matchColl.Count; i++)
            {
                dimensions[i] = Int32.Parse(matchColl[i].Groups[1].Value) + 1;
            }
            return dimensions;
        }
    }


    
}