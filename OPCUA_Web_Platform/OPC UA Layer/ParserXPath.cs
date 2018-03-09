using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Opc.Ua;
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
        public String Parse(string descriptionId, ExtensionObject extensionObject, ServiceMessageContext context)
        {
            _bd = new BinaryDecoder((byte[])extensionObject.Body, context);
            
            var serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(BuildJsonForObject(descriptionId), serializerSettings);
        }

        /// <summary>
        /// Funzione per creare il JSON dell'oggetto complesso
        /// </summary>
        /// <param name="descriptionId"></param>
        /// <returns></returns>
        private JToken BuildJsonForObject(string descriptionId)
        {
            var complexObj = new JObject();

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
                            complexObj[fieldName] = BuildInnerComplex(type.Split(':')[1], l);
                        }
                        else
                        {
                            complexObj[fieldName] = BuildSimple(type.Split(':')[1], l);
                        }
                    }
                }
            }

            return complexObj;
        }

        private int LengthField(string lengthFieldSource, JToken currentJson)
        {
            if (string.IsNullOrEmpty(lengthFieldSource)) return 1;
            Console.WriteLine($"Source length -> {lengthFieldSource}");
            return int.Parse((string)currentJson[lengthFieldSource]);

        }
        
        private JToken BuildSimple(string type, int length)
        {
            if (length == 1) return JToken.FromObject(ReadBuiltinValue(type));
            
            var a = new List<object>();

            for (int i = 0; i < length; i++)
            {
                a.Add(ReadBuiltinValue(type));
            }
            
            return JToken.FromObject(a);
        }

        private JToken BuildInnerComplex(string description, int length)
        {
            if (length == 1) return BuildJsonForObject(description);
            
            var jArray = new JArray();
            
            for (int i = 0; i < length; i++)
            {
                jArray.Insert(i, BuildJsonForObject(description));
            }

            return jArray;
        }
        
        /// <summary>
        /// Read a Built-in value starting from the current cursor in the _bd BinaryDecoder
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private Object ReadBuiltinValue(string type)
        {
            if (type.Equals("CharArray"))
                type = "String";


            var methodToCall = "Read" + type;
            MethodInfo mInfo = typeof(BinaryDecoder).GetMethod(methodToCall, new[] { typeof(string) });

            return mInfo.Invoke(_bd, new object[] { "" });
        }

    }
}