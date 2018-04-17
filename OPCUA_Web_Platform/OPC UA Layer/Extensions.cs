using Newtonsoft.Json.Linq;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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

    public static class JSONExtensionMethods
    {
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

        public static object DeserializeDataType(this JObject jObject, Type systemType)
        {
            var value = Activator.CreateInstance(systemType);
            foreach (var propertyInfo in systemType.GetProperties())
            {
                if (!propertyInfo.CanWrite || !propertyInfo.SetMethod.IsPublic) 
                    continue;
                if(!jObject.ContainsKey(propertyInfo.Name)) 
                    throw new ValueToWriteTypeException("Property " + propertyInfo.Name + " expected in the JSON Object");

                /*
                if(propertyInfo.PropertyType.IsPrimitive || propertyInfo.PropertyType == typeof(String))
                    propertyInfo.SetValue(value, JsonConvert.DeserializeObject(nestedJToken.ToString(),propertyInfo.PropertyType));
                else if (nestedJToken.Type == JTokenType.Object)
                    propertyInfo.SetValue(value, nestedJToken.ToObject<JObject>().DeserializeDataType(propertyInfo.PropertyType));
                else
                    throw new ValueToWriteTypeException("Expected Object but received " + nestedJToken.Type);*/
            }
            return value;
        }
        
        
    }
    
}