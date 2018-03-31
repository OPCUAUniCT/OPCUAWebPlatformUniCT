using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Text.RegularExpressions;

namespace WebPlatform.Extensions
{
    public static class ExpandedNdeIdExtension
    {
        public static string ToStringId(this ExpandedNodeId expandedNodeId, NamespaceTable namespaceTable)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, namespaceTable);
            return $"{nodeId.NamespaceIndex}-{nodeId.Identifier}";
        }


        //Return the array [-1] if the JToken is not an array
        public static int[] GetDimensions(this JToken jToken)
        {
            if (jToken.Type != JTokenType.Array) return new int[1] { -1 };
            bool isLast = false;
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

        public static JTokenType GetInnermostTypeForJTokenArray(this JToken jToken)
        {
            if (jToken.Type != JTokenType.Array) return JTokenType.Undefined;
            bool isLast = false;
            while (!isLast)
            {
                jToken = jToken.Last;
                isLast = jToken.Type != JTokenType.Array;
            }
            return jToken.Type;
        }

        /// <summary>
        /// This method return the @JTokenType of this JToken Array.
        /// </summary>
        /// <param name="jTokens"></param>
        /// <returns>The @JTokenType of this array. In case of several JTokenTypes it returns @JTokenType.Undefined </returns>
        public static JTokenType GetArrayType(this JToken[] jTokens)
        {
            JTokenType jtp = jTokens[0].Type;
            foreach(JToken jt in jTokens)
            {
                if (jt.Type != jtp)
                    return JTokenType.Undefined;
            }
            return jtp;
        }

    }


    
}