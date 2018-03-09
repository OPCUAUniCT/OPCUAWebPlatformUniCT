using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema;
using Opc.Ua;
using Opc.Ua.Client;
using WebPlatform.Extensions;
using WebPlatform.Models.OPCUA;

namespace WebPlatform.OPCUALayer
{
    public class DataTypeManager
    {
        private Session m_session;
        
        public DataTypeManager(Session session)
        {
            m_session = session;
        }
        
        public UaValue GetUaValue(VariableNode variableNode)
        {
            #region local variables
            bool isScalar = false;
            JObject valueOut = new JObject();
            dynamic valueOutBuiltIn;
            JSchemaGenerator gen = new JSchemaGenerator();
            JSchema SchemaOut = new JSchema();
            #endregion
            
            DataValue dataValue = m_session.ReadValue(variableNode.NodeId);
            
            /* Check if the value can be mapped directly as JSON base type */
            
            //Get the value
            var value = new Variant(dataValue.Value);
            //Check if it is a scalar
            isScalar = variableNode.ValueRank == -1;
            //Get tha Built-In type to the relevant DataType
            //TODO: verificare se funziona anche levando il TypeTable
            BuiltInType type = TypeInfo.GetBuiltInType(variableNode.DataType, m_session.SystemContext.TypeTable);

            switch (type)
            {
                    case BuiltInType.Boolean:
                        return SerializeBoolean(variableNode, value);
                    case BuiltInType.SByte:
                        return SerializeSByte(variableNode, value);
                    case BuiltInType.Byte:
                        return SerializeByte(variableNode, value);
                    case BuiltInType.Int16: case BuiltInType.UInt16:
                    case BuiltInType.Int32: case BuiltInType.UInt32:
                    case BuiltInType.Int64: case BuiltInType.UInt64:
                        return SerializeInteger(variableNode, value);
                    case BuiltInType.Float:
                        return SerializeFloat(variableNode, value);
                    case BuiltInType.Double:
                        return SerializeDouble(variableNode, value);
                    case BuiltInType.String:  case BuiltInType.DateTime: 
                    case BuiltInType.Guid:    case BuiltInType.DiagnosticInfo:
                        return SerializeString(variableNode, value);
                    case BuiltInType.LocalizedText:
                        return SerializeLocalizedText(variableNode, value);
                    case BuiltInType.NodeId: 
                        return SerializeNodeId(variableNode, value);
                    case BuiltInType.ExpandedNodeId:
                        return SerializeExpandedNodeId(variableNode, value);
                    case BuiltInType.StatusCode:
                        return SerializeStatusCode(variableNode, value);
                    case BuiltInType.QualifiedName:
                        return SerializeQualifiedName(variableNode, value);
                    case BuiltInType.XmlElement:
                        return SerializeXmlElement(variableNode, value);
                    case BuiltInType.ByteString:
                        return SerializeByteString(variableNode, value);
                    case BuiltInType.Enumeration:
                        return SerializeEnumeration(variableNode, value);
                    case BuiltInType.ExtensionObject:
                        return SerializeExtensionObject(variableNode, value);
            }

            return null;
        }

        private UaValue SerializeBoolean(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();
            
            if (variableNode.ValueRank == -1)
            {
                var jBoolVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(Boolean));
                return new UaValue(jBoolVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
               
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Boolean });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArray = JArray.Parse(arrStr);
                
                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Boolean });

                return new UaValue(jArray, outerSchema);
            }
        }

        private UaValue SerializeInteger(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(int));
                return new UaValue(jIntVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Integer });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Integer });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeByte(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(int));
                return new UaValue(jIntVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {                
                var bytes = (Byte[])value.Value;
                int[] byteRepresentations = new int[bytes.Length];
                for (int i = 0; i < bytes.Length; i++)
                {
                    byteRepresentations[i] = Convert.ToInt32(bytes[i]);
                }
                var jArray = new JArray(byteRepresentations);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { bytes.Length }, new JSchema { Type = JSchemaType.String });

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Integer });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeSByte(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(int));
                return new UaValue(jIntVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var bytes = (SByte[])value.Value;
                int[] byteRepresentations = new int[bytes.Length];
                for (int i = 0; i < bytes.Length; i++)
                {
                    byteRepresentations[i] = Convert.ToInt32(bytes[i]);
                }
                var jArray = new JArray(byteRepresentations);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { bytes.Length }, new JSchema { Type = JSchemaType.String });

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema { Type = JSchemaType.Integer });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeFloat(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jFloatVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(float));
                return new UaValue(jFloatVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Number });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Number });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeDouble(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jDoubleVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(double));
                return new UaValue(jDoubleVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Number });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Number });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeString(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jStringVal = new JValue(value.Value);
                var schema = schemaGenerator.Generate(typeof(string));
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.String });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.String });
                return new UaValue(jArr, outerSchema);
            }
        }
        
        private UaValue SerializeStatusCode(VariableNode variableNode, Variant value)
        {
            var innerSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "code", new JSchema
                        { 
                            Type = JSchemaType.String, 
                            Enum = { "Good", "Uncertain", "Bad" } 
                        } 
                    },
                    { "structureChanged", new JSchema{ Type = JSchemaType.Boolean } }
                }
            };
            
            if (variableNode.ValueRank == -1)
            {
                var statusValue = new PlatformStatusCode((StatusCode)value.Value);
                var jStringVal = JObject.FromObject(statusValue);

                return new UaValue(jStringVal, innerSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)((StatusCode[]) value.Value).Select(val => JObject.FromObject(new PlatformStatusCode(val))).ToArray();

                var jArray = new JArray(arr);
                
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema);
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                
                var transformedArr = IterativeCopy<StatusCode, JObject>(arr, matrix.Dimensions, i => JObject.FromObject(new PlatformStatusCode(i)));
                var arrStr = JsonConvert.SerializeObject(transformedArr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema);
                return new UaValue(jArr, outerSchema);
            }
        }
        
        private UaValue SerializeQualifiedName(VariableNode variableNode, Variant value)
        {
            var innerSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "NamespaceIndex", new JSchema{ Type = JSchemaType.Integer} },
                    { "Name", new JSchema{ Type = JSchemaType.String } }
                }
            };
            
            if (variableNode.ValueRank == -1)
            {
                var jStringVal = JObject.FromObject((QualifiedName)value.Value);

                return new UaValue(jStringVal, innerSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)((QualifiedName[]) value.Value).Select(JObject.FromObject).ToArray();
                var jArray = new JArray(arr);
                
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema);
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                
                var transformedArr = IterativeCopy<QualifiedName, JObject>(arr, matrix.Dimensions, JObject.FromObject);
                var arrStr = JsonConvert.SerializeObject(transformedArr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema);
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeNodeId(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                NodeId nodeId = (NodeId)value.Value;
                string nodeIdRepresentation = "";
                if (nodeId.IdType == IdType.Opaque)
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeId.Identifier);
                else
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + nodeId.Identifier.ToString();
                var jStringVal = new JValue(nodeIdRepresentation);
                var schema = schemaGenerator.Generate(typeof(string));
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var nodeIds = (NodeId[])value.Value;
                string[] nodeIdRepresentations = new string[nodeIds.Length];
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i].IdType == IdType.Opaque)
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeIds[i].Identifier, 0 , ((byte[])nodeIds[i].Identifier).Length);
                    else
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + nodeIds[i].Identifier.ToString();
                }
                var jArray = new JArray(nodeIdRepresentations);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { nodeIds.Length }, new JSchema { Type = JSchemaType.String });

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var nodeIds = (NodeId[])matrix.Elements;
                string[] nodeIdRepresentations = new string[nodeIds.Length];
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i].IdType == IdType.Opaque)
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeIds[i].Identifier, 0, ((byte[])nodeIds[i].Identifier).Length);
                    else
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + nodeIds[i].Identifier.ToString();
                }
                //Creo una nuova matrice con dimensioni uguali, di tipo stringa e con i valori che voglio (quelli in nodeIdRepresentations
                var arr = (new Matrix(nodeIdRepresentations,BuiltInType.String, matrix.Dimensions)).ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema { Type = JSchemaType.String });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeLocalizedText(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                LocalizedText locText = (LocalizedText)value.Value;
                var loctext = new
                {
                    locText.Locale,
                    locText.Text
                };
                var jStringVal = JObject.Parse(JsonConvert.SerializeObject(loctext));
                
                var schema = new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                };
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var jArray = new JArray();
                
                var locText = (LocalizedText[])value.Value;
                for (int i = 0; i < locText.Length; i++)
                {
                    jArray.Add(JObject.Parse(JsonConvert.SerializeObject(new
                    {
                        locText[i].Locale,
                        locText[i].Text
                    })));
                }
                
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { locText.Length }, new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                });

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var locTexts = (LocalizedText[])matrix.Elements;
                var locTextsRepresentation = new dynamic[matrix.Elements.Length];
                for (int i = 0; i < locTexts.Length; i++)
                {
                    locTextsRepresentation[i] = new
                    {
                        locTexts[i].Locale,
                        locTexts[i].Text
                    };
                }

                var arr = (new Matrix(locTextsRepresentation, BuiltInType.ExtensionObject, matrix.Dimensions)).ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                });

                return new UaValue(jArr, outerSchema);
            }
        }

        //Warning: bisogna gestire gli ExpandedNodeId quando absolute = true
        //Guardare https://github.com/OPCFoundation/UA-.NETStandard/issues/369#issuecomment-367991465
        private UaValue SerializeExpandedNodeId(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                ExpandedNodeId nodeId = (ExpandedNodeId)value.Value;
                string nodeIdRepresentation = "";
                if (nodeId.IdType == IdType.Opaque)
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeId.Identifier);
                else
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + nodeId.Identifier.ToString();
                var jStringVal = new JValue(nodeIdRepresentation);
                var schema = schemaGenerator.Generate(typeof(string));
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var nodeIds = (ExpandedNodeId[])value.Value;
                string[] nodeIdRepresentations = new string[nodeIds.Length];
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i].IdType == IdType.Opaque)
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeIds[i].Identifier, 0, ((byte[])nodeIds[i].Identifier).Length);
                    else
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + nodeIds[i].Identifier.ToString();
                }
                var jArray = new JArray(nodeIdRepresentations);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { nodeIds.Length }, new JSchema { Type = JSchemaType.String });

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                //la matrice ha un array lineare di tutti gli elementi. Lo prendo per ottenere tutti i valori della matrice
                var nodeIds = (ExpandedNodeId[])matrix.Elements;
                //Creo un array uguale a quello sopra e metto la stringa corrispondente in ogni valore
                string[] nodeIdRepresentations = new string[nodeIds.Length];
                for (int i = 0; i < nodeIds.Length; i++)
                {
                    if (nodeIds[i].IdType == IdType.Opaque)
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeIds[i].Identifier, 0, ((byte[])nodeIds[i].Identifier).Length);
                    else
                        nodeIdRepresentations[i] = nodeIds[i].NamespaceIndex + "-" + nodeIds[i].Identifier.ToString();
                }
                //Creo una nuova matrice con dimensioni uguali, di tipo stringa e con i valori che voglio (quelli in nodeIdRepresentations
                var arr = (new Matrix(nodeIdRepresentations, BuiltInType.String, matrix.Dimensions)).ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema { Type = JSchemaType.String });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeXmlElement(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();
            //TODO:The stack is not able to handle xml elements. Bug maybe.
            throw new NotImplementedException();
        }

        private UaValue SerializeByteString(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jStringVal = new JValue(BitConverter.ToString((byte[])value.Value));
                var schema = schemaGenerator.Generate(typeof(string));
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.String });
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.String });
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeEnumeration(VariableNode variableNode, Variant value)
        {
            int enstrreturn = GetEnumStrings(variableNode.DataType, out var enumString, out var enumValues);
            
            var innerSchema = new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "EnumValue", new JSchema { Type = JSchemaType.Integer } },
                    { "EnumLabel", new JSchema { Type = JSchemaType.String } }
                }
            };

            if (enstrreturn == 1)
            {
                List<JToken> list = enumString.Select(val => new JValue(val.Text)).Cast<JToken>().ToList();
                innerSchema.Properties["EnumLabel"].Enum.Add(list);
            }
            
            if (variableNode.ValueRank == -1)
            {
                var valueOut = GetEnumValue(value, enstrreturn, enumString, enumValues);
                IEnumerable<JToken> listaenum = enumString.ToList().Select(x => new JValue(x.ToString()));

                return new UaValue(valueOut, innerSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema);
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema);
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeExtensionObject(VariableNode variableNode, Variant value)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if it is not a Type of the standard information model
                if (variableNode.DataType.NamespaceIndex != 0)
                {
                    var analyzer = new DataTypeAnalyzer(m_session);
                    var encodingNodeId = analyzer.GetDataTypeEncodingNodeId(variableNode.DataType);
                    var descriptionNodeId = analyzer.GetDataTypeDescriptionNodeId(encodingNodeId);
                    //TODO: A cache for the dictionary could be implemented in order to improve performances
                    string dictionary = analyzer.GetDictionary(descriptionNodeId);
                    
                    //Retrieve a key that will be used by the Parser. As explained in the specification Part 3, 
                    //the value of DataTypeDescription variable contains the description identifier in the 
                    //DataTypeDictionary value which describe the data structure.
                    string descriptionId = ReadService(descriptionNodeId, Attributes.Value)[0].Value.ToString();
                    
                    //Start parsing
                    ParserXPath parser = new ParserXPath(dictionary);
                    
                    return parser.Parse(descriptionId, (ExtensionObject) value.Value, m_session.MessageContext);
                }
                
                var statusValue = new PlatformStatusCode((StatusCode)value.Value);
                var jStringVal = JObject.FromObject(statusValue);

                return new UaValue(jStringVal, null);
            }
            return null;
            /*else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)((StatusCode[]) value.Value).Select(val => JObject.FromObject(new PlatformStatusCode(val))).ToArray();

                var jArray = new JArray(arr);
                
                var schema = DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema);
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                
                var transformedArr = IterativeCopy<StatusCode, JObject>(arr, matrix.Dimensions, i => JObject.FromObject(new PlatformStatusCode(i)));
                var arrStr = JsonConvert.SerializeObject(transformedArr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema);
                return new UaValue(jArr, outerSchema);
            }*/
        }

        private int GetEnumStrings(NodeId dataTypeNodeId, out LocalizedText[] enumStrings, out EnumValueType[] enumValues)
        {
            ReferenceDescriptionCollection refDescriptionCollection;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                dataTypeNodeId,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HasProperty,  //HasProperty reference
                true,
                (uint)NodeClass.Variable, //looking for Variable
                out continuationPoint,
                out refDescriptionCollection);

            //Because it is enum it will reference Property (variabile) EnumStrings.

            bool enumstr = refDescriptionCollection.Exists(referenceDescription => referenceDescription.BrowseName.Name.Equals("EnumStrings"));
            bool enumval = refDescriptionCollection.Exists(referenceDescription => referenceDescription.BrowseName.Name.Equals("EnumValues"));

            if (enumstr)
            {
                ReferenceDescription enumStringsReferenceDescription = refDescriptionCollection.First(referenceDescription => referenceDescription.BrowseName.Name.Equals("EnumStrings"));
                NodeId enumStringNodeId = (NodeId)enumStringsReferenceDescription.NodeId;
                enumStrings = (LocalizedText[])ReadService(enumStringNodeId, Attributes.Value)[0].Value;
                enumValues = null;

                return 1;
            }

            if (enumval)
            {
                ReferenceDescription enumValuesReferenceDescription = refDescriptionCollection.First(referenceDescription => referenceDescription.BrowseName.Name.Equals("EnumValues"));
                NodeId enumStringNodeId = (NodeId)enumValuesReferenceDescription.NodeId;
                ExtensionObject[] enVal = (ExtensionObject[])ReadService(enumStringNodeId, Attributes.Value)[0].Value;
                enumValues = new EnumValueType[enVal.Length];
                for (int ind = 0; ind < enVal.Length; ind++)
                    enumValues[ind] = (EnumValueType)enVal[ind].Body;

                enumStrings = null;

                return 2;
            }

            enumValues = null;
            enumStrings = null;
            return -1;

        }

        private DataValueCollection ReadService(NodeId nodeId, uint attributeId)
        {
            ReadValueIdCollection nodeToRead = new ReadValueIdCollection(1);

            ReadValueId vId = new ReadValueId()
            {
                NodeId = nodeId,
                AttributeId = attributeId
            };

            nodeToRead.Add(vId);

            DataValueCollection dataValueCollection;
            DiagnosticInfoCollection diagnCollection;

            var responseRead = m_session.Read(null,
                         0,
                         TimestampsToReturn.Both,
                         nodeToRead,
                         out dataValueCollection,
                         out diagnCollection
                         );

            return dataValueCollection;
        }
        
        private static dynamic GetEnumValue(Variant value, int enstrreturn, LocalizedText[] enumString,
            EnumValueType[] enumValues)
        {
            dynamic valueOut;
            int index = (int) value.Value;
            if (enstrreturn < 0)
            {
                var jsonResultEnumerationCustom = new
                {
                    EnumValue = index,
                    EnumLabel = ""
                };
                valueOut = JObject.FromObject(jsonResultEnumerationCustom);
            }
            else if (enstrreturn == 1)
            {
                var jsonResultEnumerationCustom = new
                {
                    EnumValue = index,
                    EnumLabel = enumString[index].Text
                };
                valueOut = JObject.FromObject(jsonResultEnumerationCustom);
            }
            else
            {
                var jsonResultEnumerationCustom = new
                {
                    EnumValue = index,
                    EnumLabel = enumValues.First(enumValue => enumValue.Value.Equals(index)).DisplayName.Text
                };
                valueOut = JObject.FromObject(jsonResultEnumerationCustom);
            }

            return valueOut;
        }

        //TODO: this method is a recursive alternative to IterativeCopy. Delete it.
        private static void RecursiveCopy<TInput, TOutput>(
            Array source,
            Array destination,
            int[] dimensions,
            Func<TInput, TOutput> mutate,
            int[] indexPrefix = null)
        {
            indexPrefix = indexPrefix ?? new int[0];
            if (dimensions.Length != 1)
            {
                for (var i = 0; i < dimensions[0]; i++)
                {
                    var newDimensions = new int[dimensions.Length - 1];
                    Array.Copy(dimensions, 1, newDimensions, 0, dimensions.Length - 1);
    
                    var newIndexPrefix = new int[indexPrefix.Length + 1];
                    Array.Copy(indexPrefix, 0, newIndexPrefix, 0, indexPrefix.Length);
                    newIndexPrefix[indexPrefix.Length] = i;
    
                    RecursiveCopy(source, destination, newDimensions, mutate, newIndexPrefix);
                }
            }
            else
            {
                var currentIndex = new int[indexPrefix.Length + 1];
                Array.Copy(indexPrefix, 0, currentIndex, 0, indexPrefix.Length);
                for (var i = 0; i < dimensions[0]; i++)
                {
                    currentIndex[indexPrefix.Length] = i;
                    var value = source.GetValue(currentIndex);
                    if (value is TInput input)
                    {
                        var mutated = mutate(input);
                        destination.SetValue(mutated, currentIndex);
                    }
                    else
                    {
                        throw new ArgumentException("Different type. Expected " + nameof(TInput));
                    }
                }
            }
        }

        private static Array IterativeCopy<TInput, TOutput>(Array source, int[] dimensions, Func<TInput, TOutput> mutate)
        {
            var array = Array.CreateInstance(typeof(TOutput), dimensions);
            var flatSource = Utils.FlattenArray(source);
            var indexes = new int[dimensions.Length];

            for (var ii = 0; ii < flatSource.Length; ii++)
            {
                var mutated = mutate((TInput)flatSource.GetValue(ii));
                array.SetValue(mutated, indexes);
                
                for (var jj = indexes.Length-1; jj >= 0; jj--)
                {
                    indexes[jj]++;
                    
                    if (indexes[jj] < dimensions[jj])
                    {
                        break;
                    }

                    indexes[jj] = 0;
                }
            }

            return array;
        }
    }

    internal class PlatformStatusCode
    {
        public readonly string code;
        public readonly bool structureChanged;

        public PlatformStatusCode(StatusCode statusCode)
        {
            code = Regex.Match(statusCode.ToString(), @"(Good|Uncertain|Bad)").Groups[1].ToString();
            structureChanged = statusCode.StructureChanged;
        }
    }
}

namespace WebPlatform.Extensions
{
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
}