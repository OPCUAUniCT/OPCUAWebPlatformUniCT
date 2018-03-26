using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Opc.Ua;
using WebPlatform.Models.OPCUA;
using Formatting = Newtonsoft.Json.Formatting;

namespace WebPlatform.OPCUALayer
{
    public class ParserXPath
    {
        private readonly XPathNavigator _nav;
        private readonly XmlNamespaceManager _ns;
        private BinaryDecoder _bd;

        public ParserXPath(string dict)
        {
            using (TextReader sr = new StringReader(dict))
            {
                var pathDoc = new XPathDocument(sr);
                _nav = pathDoc.CreateNavigator();
                
                //add all xmlns to namespaceManager.
                _nav.MoveToFollowing(XPathNodeType.Element);
                
                IDictionary<string, string> namespaces = _nav.GetNamespacesInScope(XmlNamespaceScope.All);
                _ns = new XmlNamespaceManager(_nav.NameTable);

                foreach (KeyValuePair<string, string> entry in namespaces)
                    _ns.AddNamespace(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Function that parses complex types
        /// </summary>
        /// <param name="descriptionId"></param>
        /// <param name="extensionObject"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public UaValue Parse(string descriptionId, ExtensionObject extensionObject, ServiceMessageContext context, bool generateSchema)
        {
            _bd = new BinaryDecoder((byte[])extensionObject.Body, context);
            
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            //return JsonConvert.SerializeObject(BuildJsonForObject(descriptionId), serializerSettings);
            return BuildJsonForObject(descriptionId, generateSchema);
        }

        private UaValue BuildJsonForObject(string descriptionId, bool generateSchema)
        {
            var complexObj = new JObject();
            var complexSchema = generateSchema ? new JSchema { Type = JSchemaType.Object } : null;

            XPathNodeIterator iterator = _nav.Select($"/opc:TypeDictionary/opc:StructuredType[@Name='{descriptionId}']", _ns);

            while(iterator.MoveNext())
            {
                XPathNodeIterator newIterator = iterator.Current.SelectDescendants(XPathNodeType.Element, matchSelf: false);
                while(newIterator.MoveNext())
                {
                    if (newIterator.Current.Name.Equals("opc:Field"))
                    {
                        string fieldName = newIterator.Current.GetAttribute("Name", "");
                        string type = newIterator.Current.GetAttribute("TypeName", "");
                        string lengthSource = newIterator.Current.GetAttribute("LengthField", "");

                        int l = LengthField(lengthSource, complexObj);

                        if (!(type.Contains("opc:") || type.Contains("ua:")))
                        {
                            var uaValue = BuildInnerComplex(type.Split(':')[1], l, generateSchema);
                            complexObj[fieldName] = uaValue.Value;
                            if (generateSchema)
                            {
                                complexSchema.Properties.Add(fieldName, uaValue.Schema);
                            }
                        }
                        else
                        {
                            var uaValue = BuildSimple(type.Split(':')[1], l, generateSchema);
                            complexObj[fieldName] = uaValue.Value;
                            if (generateSchema)
                            {
                                complexSchema.Properties.Add(fieldName, uaValue.Schema);
                            }
                        }
                    }
                }
            }

            return new UaValue(complexObj, complexSchema);
        }

        private int LengthField(string lengthFieldSource, JToken currentJson)
        {
            if (string.IsNullOrEmpty(lengthFieldSource)) return 1;
            Console.WriteLine($"Source length -> {lengthFieldSource}");
            return int.Parse((string)currentJson[lengthFieldSource]);

        }
        
        private UaValue BuildSimple(string type, int length, bool generateSchema)
        {
            var builtinType = DataTypeAnalyzer.GetBuiltinTypeFromStandardTypeDescription(type);
            var jSchema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForStandardTypeDescription(builtinType) : null;
            
            if (length == 1)
            {
                var jValue = JToken.FromObject(ReadBuiltinValue(builtinType));
                return new UaValue(jValue, jSchema);
            }
            
            var a = new List<object>();

            for (int i = 0; i < length; i++)
            {
                a.Add(ReadBuiltinValue(builtinType));
            }

            var arrSchema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {length}, jSchema) : null;
            
            return new UaValue(JToken.FromObject(a), arrSchema);
        }

        private UaValue BuildInnerComplex(string description, int length, bool generateSchema)
        {
            if (length == 1) return BuildJsonForObject(description, generateSchema);
            
            var jArray = new JArray();
            UaValue uaVal = new UaValue();
            
            for (int i = 0; i < length; i++)
            {
                uaVal = BuildJsonForObject(description, generateSchema);
                jArray.Insert(i, uaVal.Value);
            }

            var jSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {length}, uaVal.Schema) : null;

            return new UaValue(jArray, jSchema);
        }
        
        /// <summary>
        /// Read a Built-in value starting from the current cursor in the _bd BinaryDecoder
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Object ReadBuiltinValue(BuiltInType builtinType)
        {
            var methodToCall = "Read" + builtinType;

            MethodInfo mInfo = typeof(BinaryDecoder).GetMethod(methodToCall, new[] { typeof(string) });
            if (builtinType == BuiltInType.ByteString)
            {
                Object a = mInfo.Invoke(_bd, new object[] { "" });
                byte[] b = a as byte[];
                var c = Convert.ToBase64String(b);
                return c;
            }
                

            return mInfo.Invoke(_bd, new object[] { "" });
        }

    }
}