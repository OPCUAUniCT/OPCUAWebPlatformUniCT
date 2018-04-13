using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema;
using Opc.Ua;
using Opc.Ua.Client;
using WebPlatform.Exceptions;
using WebPlatform.Extensions;
using WebPlatform.Models.DataSet;
using WebPlatform.Models.OPCUA;
using WebPlatform.OPC_UA_Layer;

namespace WebPlatform.OPCUALayer
{
    public class DataTypeManager
    {
        private readonly Session _session;
        
        public DataTypeManager(Session session)
        {
            _session = session;
        }

        #region Read UA Value

        public UaValue GetUaValue(VariableNode variableNode, bool generateSchema = true)
        {
            DataValue dataValue = _session.ReadValue(variableNode.NodeId);
            var uaValue = GetUaValue(variableNode, dataValue, generateSchema);
            uaValue.StatusCode = dataValue.StatusCode;
            return uaValue;
        }

        public UaValue GetUaValue(VariableNode variableNode, DataValue dataValue, bool generateSchema)
        {
            var value = new Variant(dataValue.Value);
            BuiltInType type = TypeInfo.GetBuiltInType(variableNode.DataType, _session.SystemContext.TypeTable);

            switch (type)
            {
                    case BuiltInType.Boolean:
                        return SerializeBoolean(variableNode, value, generateSchema);
                    case BuiltInType.SByte:
                        return SerializeSByte(variableNode, value, generateSchema);
                    case BuiltInType.Byte:
                        return SerializeByte(variableNode, value, generateSchema);
                    case BuiltInType.Int16: case BuiltInType.UInt16:
                    case BuiltInType.Int32: case BuiltInType.UInt32:
                    case BuiltInType.Int64: case BuiltInType.UInt64:
                        return SerializeInteger(variableNode, value, generateSchema);
                    case BuiltInType.Float:
                        return SerializeFloat(variableNode, value, generateSchema);
                    case BuiltInType.Double:
                        return SerializeDouble(variableNode, value, generateSchema);
                    case BuiltInType.String:  case BuiltInType.DateTime: 
                    case BuiltInType.Guid:    case BuiltInType.DiagnosticInfo:
                        return SerializeString(variableNode, value, generateSchema);
                    case BuiltInType.LocalizedText:
                        return SerializeLocalizedText(variableNode, value, generateSchema);
                    case BuiltInType.NodeId: 
                        return SerializeNodeId(variableNode, value, generateSchema);
                    case BuiltInType.ExpandedNodeId:
                        return SerializeExpandedNodeId(variableNode, value, generateSchema);
                    case BuiltInType.StatusCode:
                        return SerializeStatusCode(variableNode, value, generateSchema);
                    case BuiltInType.QualifiedName:
                        return SerializeQualifiedName(variableNode, value, generateSchema);
                    case BuiltInType.XmlElement:
                        return SerializeXmlElement(variableNode, value, generateSchema);
                    case BuiltInType.ByteString:
                        return SerializeByteString(variableNode, value, generateSchema);
                    case BuiltInType.Enumeration:
                        return SerializeEnumeration(variableNode, value, generateSchema);
                    case BuiltInType.ExtensionObject:
                        return SerializeExtensionObject(variableNode, value, generateSchema);
            }

            return null;
        }

        private UaValue SerializeBoolean(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();
            
            if (variableNode.ValueRank == -1)
            {
                var jBoolVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(Boolean)) : null;
                return new UaValue(jBoolVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
               
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Boolean }) :
                    null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArray = JArray.Parse(arrStr);
                
                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Boolean }) : 
                    null;

                return new UaValue(jArray, outerSchema);
            }
        }

        private UaValue SerializeInteger(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(int)) : null;
                return new UaValue(jIntVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Integer }) : 
                    null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Integer }) : 
                    null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeByte(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(int)) : null;
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
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] { bytes.Length }, new JSchema { Type = JSchemaType.String }) : 
                    null;

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Integer }) : 
                    null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeSByte(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(int)) : null;
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
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] { bytes.Length }, new JSchema { Type = JSchemaType.String }) : null;

                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema { Type = JSchemaType.Integer }) : null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeFloat(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jFloatVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(float)) : null;
                return new UaValue(jFloatVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Number }) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Number }) : null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeDouble(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jDoubleVal = new JValue(value.Value);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(double)) : null;
                return new UaValue(jDoubleVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.Number }) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.Number }) : null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeString(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jStringVal = new JValue(value.Value.ToString());
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(string)) : null;
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.String }) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema{ Type = JSchemaType.String }) : null;
                return new UaValue(jArr, outerSchema);
            }
        }
        
        private UaValue SerializeStatusCode(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var innerSchema = (generateSchema) ? new JSchema
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
            } : null;
            
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
                
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                
                var transformedArr = IterativeCopy<StatusCode, JObject>(arr, matrix.Dimensions, i => JObject.FromObject(new PlatformStatusCode(i)));
                var arrStr = JsonConvert.SerializeObject(transformedArr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema) : null;
                return new UaValue(jArr, outerSchema);
            }
        }
        
        private UaValue SerializeQualifiedName(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var innerSchema = (generateSchema) ? new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "NamespaceIndex", new JSchema{ Type = JSchemaType.Integer} },
                    { "Name", new JSchema{ Type = JSchemaType.String } }
                }
            } : null;
            
            if (variableNode.ValueRank == -1)
            {
                var jStringVal = JObject.FromObject((QualifiedName)value.Value);

                return new UaValue(jStringVal, innerSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)((QualifiedName[]) value.Value).Select(JObject.FromObject).ToArray();
                var jArray = new JArray(arr);
                
                var schema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                
                var transformedArr = IterativeCopy<QualifiedName, JObject>(arr, matrix.Dimensions, JObject.FromObject);
                var arrStr = JsonConvert.SerializeObject(transformedArr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = (generateSchema) ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, innerSchema) : null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeNodeId(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                NodeId nodeId = (NodeId)value.Value;
                string nodeIdRepresentation = "";
                if (nodeId.IdType == IdType.Opaque)
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + Convert.ToBase64String((byte[])nodeId.Identifier);
                else
                    nodeIdRepresentation = nodeId.NamespaceIndex + "-" + nodeId.Identifier;
                var jStringVal = new JValue(nodeIdRepresentation);
                var schema = (generateSchema) ? schemaGenerator.Generate(typeof(string)) : null;
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
                var schema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] { nodeIds.Length }, new JSchema { Type = JSchemaType.String }) : null;

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
                var arr = (new Matrix(nodeIdRepresentations,BuiltInType.String, matrix.Dimensions)).ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var outerSchema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema { Type = JSchemaType.String }) : null;
                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeLocalizedText(VariableNode variableNode, Variant value, bool generateSchema)
        {
            if (variableNode.ValueRank == -1)
            {
                LocalizedText locText = (LocalizedText)value.Value;
                var loctext = new
                {
                    locText.Locale,
                    locText.Text
                };
                var jStringVal = JObject.Parse(JsonConvert.SerializeObject(loctext));
                
                var schema = generateSchema ? new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                } : null;
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
                
                var schema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { locText.Length }, new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                }) : null;

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

                var outerSchema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, new JSchema()
                {
                    Type = JSchemaType.Object,
                    Properties = {
                        { "Locale", new JSchema { Type = JSchemaType.String } },
                        { "Text", new JSchema { Type = JSchemaType.String } }
                    }
                }) : null;

                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeExpandedNodeId(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schema = (generateSchema) ? new JSchema()
            {
                Type = JSchemaType.Object,
                Properties = {
                        { "NodeId", new JSchema { Type = JSchemaType.String } },
                        { "NamespaceUri", new JSchema { Type = JSchemaType.String } },
                        { "ServerIndex", new JSchema { Type = JSchemaType.Integer } }
                    }
            } : null;

            if (variableNode.ValueRank == -1)
            {
                ExpandedNodeId expandedNodeId = (ExpandedNodeId)value.Value;
                string NodeId = "";
                if (expandedNodeId.IdType == IdType.Opaque)
                    NodeId = expandedNodeId.NamespaceIndex + "-" + Convert.ToBase64String((byte[])expandedNodeId.Identifier);
                else
                    NodeId = expandedNodeId.NamespaceIndex + "-" + expandedNodeId.Identifier;
                string NamespaceUri = "";
                if (expandedNodeId.NamespaceUri != null)
                    NamespaceUri = expandedNodeId.NamespaceUri;
                var expNodeId = new
                {
                    NodeId,
                    NamespaceUri,
                    expandedNodeId.ServerIndex
                };
                var jStringVal = JObject.Parse(JsonConvert.SerializeObject(expNodeId));

                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                JArray jArray = new JArray();
                var expandedNodeIds = (ExpandedNodeId[])value.Value;

                string NodeId;
                string NamespaceUri = "";
                for (int i = 0; i < expandedNodeIds.Length; i++)
                {
                    if (expandedNodeIds[i].IdType == IdType.Opaque)
                        NodeId = expandedNodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])expandedNodeIds[i].Identifier, 0, ((byte[])expandedNodeIds[i].Identifier).Length);
                    else
                        NodeId = expandedNodeIds[i].NamespaceIndex + "-" + expandedNodeIds[i].Identifier;
                    NamespaceUri = "";
                    if (expandedNodeIds[i].NamespaceUri != null)
                        NamespaceUri = expandedNodeIds[i].NamespaceUri;
                    var expNodeId = new
                    {
                        NodeId,
                        NamespaceUri,
                        expandedNodeIds[i].ServerIndex
                    };
                    jArray.Add(JObject.Parse(JsonConvert.SerializeObject(expNodeId)));
                }
                return new UaValue(jArray, DataTypeSchemaGenerator.GenerateSchemaForArray(new int[] { expandedNodeIds.Length }, schema));
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var expandedNodeIds = (ExpandedNodeId[])matrix.Elements;
                var expNodeIdRepresentation = new dynamic[matrix.Elements.Length];
                string NodeId;
                string NamespaceUri = "";
                for (int i = 0; i < expandedNodeIds.Length; i++)
                {
                    if (expandedNodeIds[i].IdType == IdType.Opaque)
                        NodeId = expandedNodeIds[i].NamespaceIndex + "-" + Convert.ToBase64String((byte[])expandedNodeIds[i].Identifier, 0, ((byte[])expandedNodeIds[i].Identifier).Length);
                    else
                        NodeId = expandedNodeIds[i].NamespaceIndex + "-" + expandedNodeIds[i].Identifier.ToString();

                    NamespaceUri = "";
                    if (expandedNodeIds[i].NamespaceUri != null)
                        NamespaceUri = expandedNodeIds[i].NamespaceUri;
                    var expNodeId = new
                    {
                        NodeId,
                        NamespaceUri,
                        expandedNodeIds[i].ServerIndex
                    };
                    expNodeIdRepresentation[i] = JObject.Parse(JsonConvert.SerializeObject(expNodeId));
                }

                var arr = (new Matrix(expNodeIdRepresentation, BuiltInType.ExtensionObject, matrix.Dimensions)).ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);
                return new UaValue(jArr, DataTypeSchemaGenerator.GenerateSchemaForArray(matrix.Dimensions, schema));
            }
        }

        private UaValue SerializeXmlElement(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();
            //TODO:The stack is not able to handle xml elements. Bug maybe.
            throw new NotImplementedException();
        }

        private UaValue SerializeByteString(VariableNode variableNode, Variant value, bool generateSchema)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                //var jStringVal = new JValue(BitConverter.ToString((byte[])value.Value));
                var jStringVal = new JValue(Convert.ToBase64String((byte[])value.Value));
                var schema = generateSchema ? schemaGenerator.Generate(typeof(string)) : null;
                return new UaValue(jStringVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var newArr = new List<string>();
                foreach (var byteString in arr)
                {
                    //newArr.Add(BitConverter.ToString((byte[])byteString));
                    newArr.Add(Convert.ToBase64String((byte[])byteString));
                }
                var jArray = new JArray(newArr);
                var schema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, new JSchema{ Type = JSchemaType.String }) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private UaValue SerializeEnumeration(VariableNode variableNode, Variant value, bool generateSchema)
        {
            int enstrreturn = GetEnumStrings(variableNode.DataType, out var enumString, out var enumValues);
            
            var innerSchema = generateSchema ? new JSchema
            {
                Type = JSchemaType.Object,
                Properties =
                {
                    { "EnumIndex", new JSchema { Type = JSchemaType.Integer } },
                    { "EnumValue", new JSchema { Type = JSchemaType.String } }
                }
            } : null;

            if(enstrreturn == 1)
            {
                    List<JToken> enumIndexList = new List<JToken>();
                    for (int i = 0; i < enumString.Length; i++)
                    {
                        enumIndexList.Add(new JValue(i));
                    }
                    List<JToken> enumValueList = enumString.Select(val => new JValue(val.Text)).Cast<JToken>().ToList();
                    innerSchema.Properties["EnumIndex"].Enum.Add(enumIndexList);
                    innerSchema.Properties["EnumValue"].Enum.Add(enumValueList);
            }

            if (enstrreturn == 2)
            {
                List<JToken> enumIndexList = new List<JToken>();
                List<JToken> enumValueList = new List<JToken>();
                foreach(var val in enumValues)
                {
                    enumIndexList.Add(new JValue(val.Value));
                    enumValueList.Add(new JValue(val.DisplayName.Text));
                }
                innerSchema.Properties["EnumIndex"].Enum.Add(enumIndexList);
                innerSchema.Properties["EnumValue"].Enum.Add(enumValueList);
            }

            if (variableNode.ValueRank == -1)
            {
                var valueOut = GetEnumValue(value, enstrreturn, enumString, enumValues);

                return new UaValue(valueOut, innerSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Int32[])value.Value;
                var values = arr.Select(s => GetEnumValue(s, enstrreturn, enumString, enumValues));
                var jArray = new JArray(values);
                
                var schema = generateSchema ?  DataTypeSchemaGenerator.GenerateSchemaForArray(new[] {arr.Length}, innerSchema) : null;
                
                return new UaValue(jArray, schema);
            }
            else
            {
                throw new NotImplementedException("Read Matrix of Emuneration not implemented");
            }
        }

        private UaValue SerializeExtensionObject(VariableNode variableNode, Variant value, bool generateSchema)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if it is not a Type of the standard information model
                if (variableNode.DataType.NamespaceIndex != 0)
                {
                    var analyzer = new DataTypeAnalyzer(_session);
                    var encodingNodeId = analyzer.GetDataTypeEncodingNodeId(variableNode.DataType);
                    var descriptionNodeId = analyzer.GetDataTypeDescriptionNodeId(encodingNodeId);
                    string dictionary = analyzer.GetDictionary(descriptionNodeId);
                    
                    //Retrieve a key that will be used by the Parser. As explained in the specification Part 3, 
                    //the value of DataTypeDescription variable contains the description identifier in the 
                    //DataTypeDictionary value which describe the data structure.
                    string descriptionId = ReadService(descriptionNodeId, Attributes.Value)[0].Value.ToString();
                    
                    //Start parsing
                    var parser = new ParserXPath(dictionary);
                    
                    return parser.Parse(descriptionId, (ExtensionObject) value.Value, _session.MessageContext, generateSchema);
                }

                var structStandard = ((ExtensionObject)value.Value).Body;
                var jValue = JObject.FromObject(structStandard);
                var schema4 = generateSchema ? JsonSchema4.FromSampleJson(jValue.ToString()) : null;
                var jSchema = generateSchema ? JSchema.Parse(schema4.ToJson()) : null;
                return new UaValue(jValue, jSchema);
            }
            else if (variableNode.ValueRank == 1)
            {
                if (variableNode.DataType.NamespaceIndex != 0)
                {
                    var analyzer = new DataTypeAnalyzer(_session);
                    var encodingNodeId = analyzer.GetDataTypeEncodingNodeId(variableNode.DataType);
                    var descriptionNodeId = analyzer.GetDataTypeDescriptionNodeId(encodingNodeId);
                    //TODO: A cache for the dictionary could be implemented in order to improve performances
                    string dictionary = analyzer.GetDictionary(descriptionNodeId);
                    
                    string descriptionId = ReadService(descriptionNodeId, Attributes.Value)[0].Value.ToString();
                    
                    var parser = new ParserXPath(dictionary);
                    var jArray = new JArray();
                    var arrayValue = (Array)value.Value;

                    var uaValue = new UaValue();
                    
                    foreach(var x in arrayValue)
                    {
                        uaValue = parser.Parse(descriptionId, (ExtensionObject) x, _session.MessageContext, generateSchema);
                        jArray.Add(uaValue.Value);
                    }
                    var jSchema = generateSchema ? DataTypeSchemaGenerator.GenerateSchemaForArray(new[]{arrayValue.Length}, uaValue.Schema) : null;
                    return new UaValue(jArray, jSchema);
                }
                else
                {
                    var structArray = ((ExtensionObject[])value.Value).Select(s=> s.Body);
                    var jArray = JArray.FromObject(structArray);
                    var schema4 = generateSchema ? JsonSchema4.FromSampleJson(jArray.ToString()) : null;
                    var jSchema = generateSchema ? JSchema.Parse(schema4.ToJson()) : null;
                    return new UaValue(jArray, jSchema);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private int GetEnumStrings(NodeId dataTypeNodeId, out LocalizedText[] enumStrings, out EnumValueType[] enumValues)
        {
            ReferenceDescriptionCollection refDescriptionCollection;
            byte[] continuationPoint;

            _session.Browse(
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
                NodeId enumStringNodeId = ExpandedNodeId.ToNodeId(enumStringsReferenceDescription.NodeId, _session.MessageContext.NamespaceUris);
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

            var responseRead = _session.Read(null,
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
                    EnumIndex = index,
                    EnumValue = ""
                };
                valueOut = JObject.FromObject(jsonResultEnumerationCustom);
            }
            else if (enstrreturn == 1)
            {
                var jsonResultEnumerationCustom = new
                {
                    EnumIndex = index,
                    EnumValue = enumString[index].Text
                };
                valueOut = JObject.FromObject(jsonResultEnumerationCustom);
            }
            else
            {

                var jsonResultEnumerationCustom = new
                {
                    EnumIndex = index,
                    EnumValue = enumValues.Single(s => s.Value == index).DisplayName.Text
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

        #endregion
        

        #region Write UA Values

        public DataValue GetDataValueFromVariableState(VariableState state, VariableNode variableNode)
        {

            BuiltInType type = TypeInfo.GetBuiltInType(variableNode.DataType, _session.SystemContext.TypeTable);

            switch (type)
            {
                case BuiltInType.Boolean:
                    return GetDataValueFromBoolean(variableNode, state);
                case BuiltInType.SByte:
                    return GetDataValueFromSByte(variableNode, state);
                case BuiltInType.Byte:
                    return GetDataValueFromByte(variableNode, state);
                case BuiltInType.Int16:
                    return GetDataValueFromInt16(variableNode, state);
                case BuiltInType.UInt16:
                    return GetDataValueFromUInt16(variableNode, state);
                case BuiltInType.Int32:
                    return GetDataValueFromInt32(variableNode, state);
                case BuiltInType.UInt32:
                    return GetDataValueFromUInt32(variableNode, state);
                case BuiltInType.Int64:
                    return GetDataValueFromInt64(variableNode, state);
                case BuiltInType.UInt64:
                    return GetDataValueFromUInt64(variableNode, state);
                case BuiltInType.Float:
                    return GetDataValueFromFloat(variableNode, state);
                case BuiltInType.Double:
                    return GetDataValueFromDouble(variableNode, state);
                case BuiltInType.String:
                    return GetDataValueFromString(variableNode, state);
                case BuiltInType.DateTime:
                    return GetDataValueFromDateTime(variableNode, state);
                case BuiltInType.Guid:
                    return GetDataValueFromGuid(variableNode, state);
                case BuiltInType.DiagnosticInfo:
                    throw new NotImplementedException("Write of DiagnosticInfo element is not implemented");
                case BuiltInType.LocalizedText:
                    return GetDataValueFromLocalizedText(variableNode, state);
                case BuiltInType.NodeId:
                    return GetDataValueFromNodeId(variableNode, state);
                case BuiltInType.ExpandedNodeId:
                    return GetDataValueFromExpandedNodeId(variableNode, state);
                case BuiltInType.StatusCode:
                    return GetDataValueFromStatusCode(variableNode, state);
                case BuiltInType.QualifiedName:
                    return GetDataValueFromQualifiedName(variableNode, state);
                case BuiltInType.XmlElement:
                    throw new NotImplementedException("Write of Xml element is not implemented");
                case BuiltInType.ByteString:
                    return GetDataValueFromByteString(variableNode, state);
                case BuiltInType.Enumeration:
                    return GetDataValueFromEnumeration(variableNode, state);
                case BuiltInType.ExtensionObject:
                    return GetDataValueFromExtensionObject(variableNode, state);
            }

            return null;
        }
        
        private DataValue GetDataValueFromBoolean(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Boolean
                if(state.Value.Type !=JTokenType.Boolean)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Boolean Value but received a JSON " + state.Value.Type);
                return new DataValue(new Variant(state.Value.ToObject<Boolean>()));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d JSON Array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Boolean
                if(flatValuesToWrite.GetArrayType() != JTokenType.Boolean)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Boolean Array as expected");
                Boolean[] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Boolean>()));
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                Boolean[] valuesToWriteArray;
                //Check if it is a monodimensional array
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Boolean)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Boolean Array as expected");
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Boolean>()));
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Boolean
                if (flatValuesToWrite.GetArrayType() != JTokenType.Boolean)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Boolean Array as expected");
                valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Boolean>()));
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Boolean, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromByte(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                Byte value = 0;
                try
                {
                    value = state.Value.ToObject<Byte>();
                }
                catch(OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                Byte[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Byte>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Byte[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Byte>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Byte>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Byte, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromSByte(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                SByte value = 0;
                try
                {
                    value = state.Value.ToObject<SByte>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                SByte[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<SByte>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                SByte[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<SByte>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<SByte>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.SByte, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromInt16(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                Int16 value = 0;
                try
                {
                    value = state.Value.ToObject<Int16>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                Int16[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int16>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Int16[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int16>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int16>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Int16, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromUInt16(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                UInt16 value = 0;
                try
                {
                    value = state.Value.ToObject<UInt16>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                UInt16[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt16>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                UInt16[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt16>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt16>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.UInt16, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromInt32(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                Int32 value = 0;
                try
                {
                    value = state.Value.ToObject<Int32>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                Int32[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int32>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Int32[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int32>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int32>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Int32, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromEnumeration(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is an Object
                if (state.Value.Type != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Object Value but received a JSON " + state.Value.Type);
                JObject jObject = state.Value.ToObject<JObject>();
                if (!jObject.ContainsKey("EnumIndex") || !jObject.ContainsKey("EnumValue"))
                    throw new ValueToWriteTypeException("Object must have the Properties \"EnumIndex\" and \"EnumValue\"");
                JToken jtIndex = jObject["EnumIndex"];
                JToken jtValue = jObject["EnumValue"];
                if (jtIndex.Type != JTokenType.Integer && jtValue.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("\"EnumIndex\" and \"EnumValue\" properties must be an Integer and a String");
                int valueToWrite = jtIndex.ToObject<Int32>();
                int enstrreturn = GetEnumStrings(variableNode.DataType, out var enumString, out var enumValues);

                if(enstrreturn == 1)
                {
                    if (enumString[valueToWrite] != jtValue.ToObject<String>() || valueToWrite < 0 || valueToWrite > enumString.Length)
                        throw new ValueToWriteTypeException("Wrong corrispondence between \"EnumIndex\" and \"EnumValue\"");
                }
                else if(enstrreturn == 2)
                {
                    var enVal = enumValues.SingleOrDefault(s => s.Value == valueToWrite);
                    if (enVal == null || enVal.DisplayName.Text != jtValue.ToObject<String>())
                        throw new ValueToWriteTypeException("Wrong corrispondence between \"EnumIndex\" and \"EnumValue\"");
                }

                return new DataValue(new Variant(valueToWrite));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                Int32[] valuesToWriteArray = new Int32[flatValuesToWrite.Length];
                JObject jObject;
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("EnumIndex") || !jObject.ContainsKey("EnumValue"))
                        throw new ValueToWriteTypeException("Object must have the Properties \"EnumIndex\" and \"EnumValue\"");
                    JToken jtIndex = jObject["EnumIndex"];
                    JToken jtValue = jObject["EnumValue"];
                    if (jtIndex.Type != JTokenType.Integer && jtValue.Type != JTokenType.String)
                        throw new ValueToWriteTypeException("\"EnumIndex\" and \"EnumValue\" properties must be an Integer and a String");
                    int valueToWrite = jtIndex.ToObject<Int32>();
                    int enstrreturn = GetEnumStrings(variableNode.DataType, out var enumString, out var enumValues);

                    if (enstrreturn == 1)
                    {
                        if (enumString[valueToWrite] != jtValue.ToObject<String>() || valueToWrite < 0 || valueToWrite > enumString.Length)
                            throw new ValueToWriteTypeException("Wrong corrispondence between \"EnumIndex\" and \"EnumValue\"");
                    }
                    else if (enstrreturn == 2)
                    {
                        var enVal = enumValues.SingleOrDefault(s => s.Value == valueToWrite);
                        if (enVal == null || enVal.DisplayName.Text != jtValue.ToObject<String>())
                            throw new ValueToWriteTypeException("Wrong corrispondence between \"EnumIndex\" and \"EnumValue\"");
                    }
                    valuesToWriteArray[i] = valueToWrite;
                }

                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                throw new NotImplementedException("Write Matrix of Enumeration not supported");
            }
        }

        private DataValue GetDataValueFromUInt32(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                UInt32 value = 0;
                try
                {
                    value = state.Value.ToObject<UInt32>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                UInt32[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt32>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                UInt32[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt32>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt32>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.UInt32, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromInt64(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                Int64 value = 0;
                try
                {
                    value = state.Value.ToObject<Int64>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                Int64[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int64>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Int64[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int64>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Int64>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Int64, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromUInt64(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                UInt64 value = 0;
                try
                {
                    value = state.Value.ToObject<UInt64>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                UInt64[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt64>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                UInt64[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt64>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<UInt64>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.UInt64, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromFloat(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Float
                if (state.Value.Type != JTokenType.Float && state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Number Value but received a JSON " + state.Value.Type);
                Single value = 0;
                try
                {
                    value = state.Value.ToObject<Single>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Float
                if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                Single[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Single>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Single[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Single>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Float
                if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Single>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Float, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromDouble(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Float
                if (state.Value.Type != JTokenType.Float && state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Number Value but received a JSON " + state.Value.Type);
                Double value = 0;
                try
                {
                    value = state.Value.ToObject<Double>();
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Float
                if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                Double[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Double>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                Double[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Double>()));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Float
                if (flatValuesToWrite.GetArrayType() != JTokenType.Float && flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Number Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Double>()));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Double, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromString(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a String
                if (state.Value.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON String Value but received a JSON " + state.Value.Type);
                return new DataValue(new Variant(state.Value.ToObject<String>()));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                String[] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<String>()));
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                    return new DataValue(new Variant(Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<String>()))));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                String[] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<String>()));
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Double, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }


        private DataValue GetDataValueFromByteString(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a String
                if (state.Value.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON String Value but received a JSON " + state.Value.Type);
                return new DataValue(new Variant(Convert.FromBase64String(state.Value.ToObject<string>())));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                byte[][] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, item => Convert.FromBase64String(item.ToObject<string>()));
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //A Matrix of ByteString should not be present
                throw new NotImplementedException();
            }
        }



        private DataValue GetDataValueFromGuid(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Strings
                if (state.Value.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON String Value but received a JSON " + state.Value.Type);
                Guid value;
                try
                {
                    value = state.Value.ToObject<Guid>();
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("String not formatted correctly. " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                Guid[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Guid>()));
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("One or more Strings in the Array not formatted correctly. " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                Guid[] valuesToWriteArray;
                //Check if it is a monodimensional array
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Guid>()));
                    }
                    catch (FormatException exc)
                    {
                        throw new ValueToWriteTypeException("One or more String in the Array not formatted correctly. " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }

                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<Guid>()));
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("Strings in the Array not formatted correctly. " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.Guid, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromDateTime(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Date
                if (state.Value.Type != JTokenType.Date)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Date String Value but received a JSON " + state.Value.Type);
                DateTime value;
                try
                {
                    value = state.Value.ToObject<DateTime>();
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("String not formatted correctly. " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Dates
                if (flatValuesToWrite.GetArrayType() != JTokenType.Date)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Date String Array as expected");
                DateTime[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<DateTime>()));
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("One or more Strings in the Array not formatted correctly. " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                DateTime[] valuesToWriteArray;
                //Check if it is a monodimensional array
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Date)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Date String Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<DateTime>()));
                    }
                    catch (FormatException exc)
                    {
                        throw new ValueToWriteTypeException("One or more String in the Array not formatted correctly. " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }

                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Dates
                if (flatValuesToWrite.GetArrayType() != JTokenType.Date)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Date String Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => item.ToObject<DateTime>()));
                }
                catch (FormatException exc)
                {
                    throw new ValueToWriteTypeException("Strings in the Array not formatted correctly. " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.DateTime, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromStatusCode(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Integer
                if (state.Value.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Integer Value but received a JSON " + state.Value.Type);
                StatusCode value;
                try
                {
                    value = new StatusCode(state.Value.ToObject<UInt32>());
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error: " + exc.Message);
                }
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                StatusCode[] valuesToWriteArray;
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => new StatusCode(state.Value.ToObject<UInt32>())));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                StatusCode[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                    try
                    {
                        valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => new StatusCode(state.Value.ToObject<UInt32>())));
                    }
                    catch (OverflowException exc)
                    {
                        throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Integer
                if (flatValuesToWrite.GetArrayType() != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Integer Array as expected");
                try
                {
                    valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => new StatusCode(state.Value.ToObject<UInt32>())));
                }
                catch (OverflowException exc)
                {
                    throw new ValueToWriteTypeException("Range Error in one or more values of the array: " + exc.Message);
                }
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.StatusCode, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromLocalizedText(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {  
                //Check if the JSON sent by user is an Object
                if (state.Value.Type != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Object Value but received a JSON " + state.Value.Type);
                LocalizedText value = null;JObject jObject = state.Value.ToObject<JObject>();
                if (!jObject.ContainsKey("Locale") || !jObject.ContainsKey("Text"))
                    throw new ValueToWriteTypeException("Object must have the Properties \"Locale\" and \"Text\"");
                JToken jtLocale = jObject["Locale"];
                JToken jtText = jObject["Text"];
                if(jtLocale.Type != jtText.Type && jtLocale.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("\"Locale\" and \"Text\" properties must be of String Type");
                value = new LocalizedText(jtLocale.ToObject<String>(), jtText.ToObject<String>());
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                LocalizedText[] valuesToWriteArray = new LocalizedText[flatValuesToWrite.Length];
                JObject jObject;
                for (int i = 0; i<flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("Locale") || !jObject.ContainsKey("Text"))
                        throw new ValueToWriteTypeException("Object must have the Properties \"Locale\" and \"Text\"");
                    JToken jtLocale = jObject["Locale"];
                    JToken jtText = jObject["Text"];
                    if (jtLocale.Type != jtText.Type && jtLocale.Type != JTokenType.String)
                        throw new ValueToWriteTypeException("\"Locale\" and \"Text\" properties must be of String Type");
                    valuesToWriteArray[i] = new LocalizedText(jtLocale.ToObject<String>(), jtText.ToObject<String>());
                }

                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                JObject jObject;
                LocalizedText[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    valuesToWriteArray = new LocalizedText[dimensions[0]];
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                    for (int i = 0; i < flatValuesToWrite.Length; i++)
                    {
                        jObject = flatValuesToWrite[i].ToObject<JObject>();
                        if (!jObject.ContainsKey("Locale") || !jObject.ContainsKey("Text"))
                            throw new ValueToWriteTypeException("Object must have the Properties \"Locale\" and \"Text\"");
                        JToken jtLocale = jObject["Locale"];
                        JToken jtText = jObject["Text"];
                        if (jtLocale.Type != jtText.Type && jtLocale.Type != JTokenType.String)
                            throw new ValueToWriteTypeException("\"Locale\" and \"Text\" properties must be of String Type");
                        valuesToWriteArray[i] = new LocalizedText(jtLocale.ToObject<String>(), jtText.ToObject<String>());
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                valuesToWriteArray = new LocalizedText[flatValuesToWrite.Length];
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("Locale") || !jObject.ContainsKey("Text"))
                        throw new ValueToWriteTypeException("Object must have the Properties \"Locale\" and \"Text\"");
                    JToken jtLocale = jObject["Locale"];
                    JToken jtText = jObject["Text"];
                    if (jtLocale.Type != jtText.Type && jtLocale.Type != JTokenType.String)
                        throw new ValueToWriteTypeException("\"Locale\" and \"Text\" properties must be of String Type");
                    valuesToWriteArray[i] = new LocalizedText(jtLocale.ToObject<String>(), jtText.ToObject<String>());
                }

                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.LocalizedText, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromNodeId(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a String
                if (state.Value.Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON String Value but received a JSON " + state.Value.Type);
                return new DataValue(new Variant(ParsePlatformNodeIdString(state.Value.ToObject<String>())));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                NodeId[] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => ParsePlatformNodeIdString(item.ToObject<String>())));
                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                if (dimensions.Length == 1)
                {
                    if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                    return new DataValue(new Variant(Array.ConvertAll(flatValuesToWrite, (item => ParsePlatformNodeIdString(item.ToObject<String>())))));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Strings
                if (flatValuesToWrite.GetArrayType() != JTokenType.String)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON String Array as expected");
                NodeId[] valuesToWriteArray = Array.ConvertAll(flatValuesToWrite, (item => ParsePlatformNodeIdString(item.ToObject<String>())));
                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.NodeId, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromExpandedNodeId(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is an Object
                if (state.Value.Type != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Object Value but received a JSON " + state.Value.Type);
                ExpandedNodeId valueToWrite = null;
                JObject jObject = state.Value.ToObject<JObject>();
                if (!jObject.ContainsKey("NodeId") || jObject["NodeId"].Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Object must have the string Property \"NodeId\"");
                if (!jObject.ContainsKey("NamespaceUri") || jObject["NamespaceUri"].Type != JTokenType.String)
                    throw new ValueToWriteTypeException("Object must have the string Property \"NamespaceUri\"");
                if (!jObject.ContainsKey("ServerIndex") || jObject["ServerIndex"].Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Object must have the integer Property \"ServerIndex\"");
                NodeId nodeId = ParsePlatformNodeIdString(jObject["NodeId"].ToObject<String>());
                valueToWrite = new ExpandedNodeId(nodeId, jObject["NamespaceUri"].ToObject<String>(), jObject["ServerIndex"].ToObject<UInt32>());

                return new DataValue(new Variant(valueToWrite));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                ExpandedNodeId[] valuesToWriteArray = new ExpandedNodeId[flatValuesToWrite.Length];
                JObject jObject;
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("NodeId") || jObject["NodeId"].Type != JTokenType.String)
                        throw new ValueToWriteTypeException("Object must have the string Property \"NodeId\"");
                    if (!jObject.ContainsKey("NamespaceUri") || jObject["NamespaceUri"].Type != JTokenType.String)
                        throw new ValueToWriteTypeException("Object must have the string Property \"NamespaceUri\"");
                    if (!jObject.ContainsKey("ServerIndex") || jObject["ServerIndex"].Type != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Object must have the integer Property \"ServerIndex\"");
                    NodeId nodeId = ParsePlatformNodeIdString(jObject["NodeId"].ToObject<String>());
                    valuesToWriteArray[i] = new ExpandedNodeId(nodeId, jObject["NamespaceUri"].ToObject<String>(), jObject["ServerIndex"].ToObject<UInt32>());
                }

                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                JObject jObject;
                ExpandedNodeId[] valuesToWriteArray;
                NodeId nodeId;
                if (dimensions.Length == 1)
                {
                    valuesToWriteArray = new ExpandedNodeId[dimensions[0]];
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                    for (int i = 0; i < flatValuesToWrite.Length; i++)
                    {
                        jObject = flatValuesToWrite[i].ToObject<JObject>();
                        if (!jObject.ContainsKey("NodeId") || jObject["NodeId"].Type != JTokenType.String)
                            throw new ValueToWriteTypeException("Object must have the string Property \"NodeId\"");
                        if (!jObject.ContainsKey("NamespaceUri") || jObject["NamespaceUri"].Type != JTokenType.String)
                            throw new ValueToWriteTypeException("Object must have the string Property \"NamespaceUri\"");
                        if (!jObject.ContainsKey("ServerIndex") || jObject["ServerIndex"].Type != JTokenType.Integer)
                            throw new ValueToWriteTypeException("Object must have the integer Property \"ServerIndex\"");
                        nodeId = ParsePlatformNodeIdString(jObject["NodeId"].ToObject<String>());
                        valuesToWriteArray[i] = new ExpandedNodeId(nodeId, jObject["NamespaceUri"].ToObject<String>(), jObject["ServerIndex"].ToObject<UInt32>());
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                valuesToWriteArray = new ExpandedNodeId[flatValuesToWrite.Length];
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("NodeId") || jObject["NodeId"].Type != JTokenType.String)
                        throw new ValueToWriteTypeException("Object must have the string Property \"NodeId\"");
                    if (!jObject.ContainsKey("NamespaceUri") || jObject["NamespaceUri"].Type != JTokenType.String)
                        throw new ValueToWriteTypeException("Object must have the string Property \"NamespaceUri\"");
                    if (!jObject.ContainsKey("ServerIndex") || jObject["ServerIndex"].Type != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Object must have the integer Property \"ServerIndex\"");
                    nodeId = ParsePlatformNodeIdString(jObject["NodeId"].ToObject<String>());
                    valuesToWriteArray[i] = new ExpandedNodeId(nodeId, jObject["NamespaceUri"].ToObject<String>(), jObject["ServerIndex"].ToObject<UInt32>());
                }

                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.ExpandedNodeId, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }

        private DataValue GetDataValueFromQualifiedName(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is an Object
                if (state.Value.Type != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Object Value but received a JSON " + state.Value.Type);
                QualifiedName valueToWrite = null;
                JObject jObject = state.Value.ToObject<JObject>();
                if (!jObject.ContainsKey("Name") || !jObject.ContainsKey("NamespaceIndex"))
                    throw new ValueToWriteTypeException("Object must have the Properties \"Name\" and \"NamespaceIndex\"");
                JToken jtName = jObject["Name"];
                JToken jtNamespaceIndex = jObject["NamespaceIndex"];
                if (jtName.Type != JTokenType.String || jtNamespaceIndex.Type != JTokenType.Integer)
                    throw new ValueToWriteTypeException("Object must have the string property \"Name\" and the integer property \"NamespaceIndex\"");
                valueToWrite = new QualifiedName(jtName.ToObject<String>(), jtNamespaceIndex.ToObject<UInt16>());
                return new DataValue(new Variant(valueToWrite));
            }
            else if (variableNode.ValueRank == 1)
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                int[] dimensions = state.Value.GetDimensions();
                //Check if it is a monodimensional array
                if (dimensions.Length != 1)
                    throw new ValueToWriteTypeException("Array dimensions error: expected 1d array but received " + dimensions.Length + "d");
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                QualifiedName[] valuesToWriteArray = new QualifiedName[flatValuesToWrite.Length];
                JObject jObject;
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("Name") || !jObject.ContainsKey("NamespaceIndex"))
                        throw new ValueToWriteTypeException("Object must have the Properties \"Name\" and \"NamespaceIndex\"");
                    JToken jtName = jObject["Name"];
                    JToken jtNamespaceIndex = jObject["NamespaceIndex"];
                    if (jtName.Type != JTokenType.String || jtNamespaceIndex.Type != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Object must have the string property \"Name\" and the integer property \"NamespaceIndex\"");
                    valuesToWriteArray[i] = new QualifiedName(jtName.ToObject<String>(), jtNamespaceIndex.ToObject<UInt16>());
                }

                return new DataValue(new Variant(valuesToWriteArray));
            }
            else
            {
                //Check if the JSON sent by user is an array
                if (state.Value.Type != JTokenType.Array)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Array but received a JSON " + state.Value.Type);
                Matrix matrixToWrite;
                int[] dimensions = state.Value.GetDimensions();
                JToken[] flatValuesToWrite = state.Value.Children().ToArray();
                //Check if it is a monodimensional array
                JObject jObject;
                QualifiedName[] valuesToWriteArray;
                if (dimensions.Length == 1)
                {
                    valuesToWriteArray = new QualifiedName[dimensions[0]];
                    if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                        throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                    for (int i = 0; i < flatValuesToWrite.Length; i++)
                    {
                        jObject = flatValuesToWrite[i].ToObject<JObject>();
                        if (!jObject.ContainsKey("Name") || !jObject.ContainsKey("NamespaceIndex"))
                            throw new ValueToWriteTypeException("Object must have the Properties \"Name\" and \"NamespaceIndex\"");
                        JToken jtName = jObject["Name"];
                        JToken jtNamespaceIndex = jObject["NamespaceIndex"];
                        if (jtName.Type != JTokenType.String || jtNamespaceIndex.Type != JTokenType.Integer)
                            throw new ValueToWriteTypeException("Object must have the string property \"Name\" and the integer property \"NamespaceIndex\"");
                        valuesToWriteArray[i] = new QualifiedName(jtName.ToObject<String>(), jtNamespaceIndex.ToObject<UInt16>());
                    }
                    return new DataValue(new Variant(valuesToWriteArray));
                }
                //Flat a multidimensional JToken Array
                for (int i = 0; i < dimensions.Length - 1; i++)
                    flatValuesToWrite = flatValuesToWrite.SelectMany(a => a).ToArray();
                //Check that all values are Object
                if (flatValuesToWrite.GetArrayType() != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: the JSON Array sent is not a JSON Object Array as expected");
                valuesToWriteArray = new QualifiedName[flatValuesToWrite.Length];
                for (int i = 0; i < flatValuesToWrite.Length; i++)
                {
                    jObject = flatValuesToWrite[i].ToObject<JObject>();
                    if (!jObject.ContainsKey("Name") || !jObject.ContainsKey("NamespaceIndex"))
                        throw new ValueToWriteTypeException("Object must have the Properties \"Name\" and \"NamespaceIndex\"");
                    JToken jtName = jObject["Name"];
                    JToken jtNamespaceIndex = jObject["NamespaceIndex"];
                    if (jtName.Type != JTokenType.String || jtNamespaceIndex.Type != JTokenType.Integer)
                        throw new ValueToWriteTypeException("Object must have the string property \"Name\" and the integer property \"NamespaceIndex\"");
                    valuesToWriteArray[i] = new QualifiedName(jtName.ToObject<String>(), jtNamespaceIndex.ToObject<UInt16>());
                }

                matrixToWrite = new Matrix(valuesToWriteArray, BuiltInType.QualifiedName, dimensions);
                return new DataValue(new Variant(matrixToWrite));
            }
        }
        
        private DataValue GetDataValueFromExtensionObject(VariableNode variableNode, VariableState state)
        {
            if (variableNode.ValueRank == -1)
            {
                //Check if the JSON sent by user is a Object
                if (state.Value.Type != JTokenType.Object)
                    throw new ValueToWriteTypeException("Wrong Type Error: Expected a JSON Object but received a JSON " + state.Value.Type);
                var analyzer = new DataTypeAnalyzer(_session);
                var encodingNodeId = analyzer.GetDataTypeEncodingNodeId(variableNode.DataType);
                var descriptionNodeId = analyzer.GetDataTypeDescriptionNodeId(encodingNodeId);
                //TODO: A cache for the dictionary could be implemented in order to improve performances
                string dictionary = analyzer.GetDictionary(descriptionNodeId);

                //Retrieve a key that will be used by the Parser. As explained in the specification Part 3, 
                //the value of DataTypeDescription variable contains the description identifier in the 
                //DataTypeDictionary value which describe the data structure.
                string descriptionId = ReadService(descriptionNodeId, Attributes.Value)[0].Value.ToString();

                StructuredEncoder structuredEncoder = new StructuredEncoder(dictionary);
                var value = structuredEncoder.BuildExtensionObjectFromJSONObject(descriptionId, state.Value.ToObject<JObject>(), _session.MessageContext, encodingNodeId);
                
                return new DataValue(new Variant(value));
            }
            else if (variableNode.ValueRank == 1)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private NodeId ParsePlatformNodeIdString(string str)
        {
            const string pattern = @"^(\d+)-(?:(\d+)|(\S+))$";
            var match = Regex.Match(str, pattern);
            var isString = match.Groups[3].Length != 0;
            var isNumeric = match.Groups[2].Length != 0;

            var idStr = (isString) ? $"s={match.Groups[3]}" : $"i={match.Groups[2]}";
            var builtStr = $"ns={match.Groups[1]};" + idStr;
            NodeId nodeId = null;
            try
            {
                nodeId = new NodeId(builtStr);
            }
            catch (ServiceResultException exc)
            {
                switch (exc.StatusCode)
                {
                    case StatusCodes.BadNodeIdInvalid:
                        throw new ValueToWriteTypeException("Wrong Type Error: String is not formatted as expected (number-yyy where yyy can be string or number or guid)");
                    default:
                        throw new ValueToWriteTypeException(exc.Message);
                }
            }
            

            return nodeId;
        }

        #endregion
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