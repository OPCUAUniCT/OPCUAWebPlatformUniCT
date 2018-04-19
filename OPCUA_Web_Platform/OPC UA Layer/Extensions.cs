using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Razor.Language;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using NJsonSchema;
using WebPlatform.Exceptions;

namespace WebPlatform.Extensions
{
    public static class ExpandedNodeIdExtensionMethods
    {
        public static string ToStringId(this ExpandedNodeId expandedNodeId, NamespaceTable namespaceTable)
        {
            var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, namespaceTable);
            return $"{nodeId.NamespaceIndex}-{nodeId.Identifier}";
        }
    }

    public static class CollectionInitializerExtensionMethods
    {
        public static void Add(this IList<JToken> list, IList<JToken> toAdd)
        {
            foreach (var a in toAdd)
            {
                list.Add(a);
            }
        }
    }

    public static class JsonExtensionMethods
    {
        public static int[] GetJsonArrayDimensions(this JToken jToken)
        {
            if (jToken.Type != JTokenType.Array) 
                throw new ValueToWriteTypeException("Expected a JSON Array but received a " + jToken.Type);
            while (jToken.HasValues)
            {
                var children = jToken.Children();
                var count = children.First().Count();
                
                //if(children.All(x => x.Count() == count)) throw new ValueToWriteTypeException("The array sent must have the same number of element in each dimension");
                
                foreach (var child in children)
                {
                    if(child.Count() != count)
                        throw new ValueToWriteTypeException("The array sent must have the same number of element in each dimension");
                }
                jToken = jToken.Last;
            }

            const string pattern = @"\[(\d+)\]";
            var regex = new Regex(pattern);
            var matchColl = regex.Matches(jToken.Path);
            var dimensions = new int[matchColl.Count];
            for (var i = 0; i < matchColl.Count; i++)
            {
                dimensions[i] = int.Parse(matchColl[i].Groups[1].Value) + 1;
            }
            return dimensions;
        }
        
        public static JArray ToMonoDimensionalJsonArray(this JToken jToken)
        {
            var dimensions = jToken.GetJsonArrayDimensions();
            return jToken.ToMonoDimensionalJsonArray(dimensions);
        }
        
        public static JArray ToMonoDimensionalJsonArray(this JToken jToken, int [] dimensions)
        {
            var flatValuesToWrite = jToken.Children().ToArray();
            for (var i = 0; i < dimensions.Length - 1; i++)
                flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();

            return new JArray(flatValuesToWrite);
        }


        /// <summary>
        /// This method return the @JTokenType of this JToken Array.
        /// </summary>
        /// <param name="jTokens"></param>
        /// <returns>The JTokenType of this array. In case of several JTokenTypes it returns @JTokenType.Undefined </returns>
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