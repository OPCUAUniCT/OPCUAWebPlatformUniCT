using System;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using NJsonSchema;
using Opc.Ua;
using Opc.Ua.Client;
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
                        break;
                    case BuiltInType.SByte: case BuiltInType.Byte:
                    case BuiltInType.Int16: case BuiltInType.UInt16:
                    case BuiltInType.Int32: case BuiltInType.UInt32:
                    case BuiltInType.Int64: case BuiltInType.UInt64:
                        return SerializeInteger(variableNode, value);
                        break;
                    case BuiltInType.Float:
                        return SerializeFloat(variableNode, value);
                        break;
                    case BuiltInType.Double:
                        return SerializeDouble(variableNode, value);
                        break;
                    case BuiltInType.String:         case BuiltInType.DateTime:      case BuiltInType.Guid:
                    case BuiltInType.DiagnosticInfo: case BuiltInType.NodeId:        case BuiltInType.ExpandedNodeId:
                    case BuiltInType.StatusCode:     case BuiltInType.QualifiedName: case BuiltInType.LocalizedText:
                        return SerializeString(variableNode, value);
                        break;
                    case BuiltInType.XmlElement:
                        return SerializeXmlElement(variableNode, value);
                        break;
                    case BuiltInType.ByteString:
                        return SerializeByteString(variableNode, value);
                        break;
                    case BuiltInType.Enumeration:
                        return SerializeEnumeration(variableNode, value);
                        break;
                    case BuiltInType.ExtensionObject:
                        break;
            }

            return null;
        }

        /// <summary>
        /// Serialize a Variant value in JSON wrapped with its schema in a UaValue object.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private UaValue SerializeBoolean(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();
            
            if (variableNode.ValueRank == -1)
            {
                var jBoolVal = new JValue(Boolean.Parse(value.Value.ToString()));
                var schema = schemaGenerator.Generate(typeof(Boolean));
                return new UaValue(jBoolVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
               
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = new JSchema
                {
                    Type = JSchemaType.Array,
                    Items = { new JSchema { Type = JSchemaType.Boolean } },
                    MinimumItems = arr.Length,
                    MaximumItems = arr.Length 
                };
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);
                
                var dimLen = matrix.Dimensions.Length;
                JSchema innerSchema = new JSchema();
                JSchema outerSchema = new JSchema();
                for (int dim = dimLen-1; dim >= 0; dim--)
                {
                    if (dim == dimLen-1)
                    {
                        innerSchema = new JSchema
                        {
                            Type = JSchemaType.Array,
                            Items = { new JSchema { Type = JSchemaType.Boolean } },
                            MinimumItems = matrix.Dimensions[dim],
                            MaximumItems = matrix.Dimensions[dim]
                        };
                    }
                    else
                    {
                        outerSchema = new JSchema
                        {
                            Type = JSchemaType.Array,
                            Items = { innerSchema },
                            MinimumItems = matrix.Dimensions[dim],
                            MaximumItems = matrix.Dimensions[dim]
                        };
                        innerSchema = outerSchema;
                    }
                }

                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeInteger(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jIntVal = new JValue(int.Parse(value.Value.ToString()));
                var schema = schemaGenerator.Generate(typeof(int));
                return new UaValue(jIntVal, schema);
            }
            else if (variableNode.ValueRank == 1)
            {
                var arr = (Array)value.Value;
                var jArray = new JArray(arr);
                var schema = new JSchema
                {
                    Type = JSchemaType.Array,
                    Items = { new JSchema { Type = JSchemaType.Integer } },
                    MinimumItems = arr.Length,
                    MaximumItems = arr.Length
                };
                return new UaValue(jArray, schema);
            }
            else
            {
                var matrix = (Matrix)value.Value;
                var arr = matrix.ToArray();
                var arrStr = JsonConvert.SerializeObject(arr);
                var jArr = JArray.Parse(arrStr);

                var dimLen = matrix.Dimensions.Length;
                JSchema innerSchema = new JSchema();
                JSchema outerSchema = new JSchema();
                for (int dim = dimLen - 1; dim >= 0; dim--)
                {
                    if (dim == dimLen - 1)
                    {
                        innerSchema = new JSchema
                        {
                            Type = JSchemaType.Array,
                            Items = { new JSchema { Type = JSchemaType.Integer } },
                            MinimumItems = matrix.Dimensions[dim],
                            MaximumItems = matrix.Dimensions[dim]
                        };
                    }
                    else
                    {
                        outerSchema = new JSchema
                        {
                            Type = JSchemaType.Array,
                            Items = { innerSchema },
                            MinimumItems = matrix.Dimensions[dim],
                            MaximumItems = matrix.Dimensions[dim]
                        };
                        innerSchema = outerSchema;
                    }
                }

                return new UaValue(jArr, outerSchema);
            }
        }

        private UaValue SerializeFloat(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jFloatVal = new JValue(float.Parse(value.Value.ToString()));
                var schema = schemaGenerator.Generate(typeof(float));
                return new UaValue(jFloatVal, schema);
            }
            else
            {
                //TODO: Gestire il caso degli array
                throw new NotImplementedException();
            }
        }

        private UaValue SerializeDouble(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jDoubleVal = new JValue(double.Parse(value.Value.ToString()));
                var schema = schemaGenerator.Generate(typeof(double));
                return new UaValue(jDoubleVal, schema);
            }
            else
            {
                //TODO: Gestire il caso degli array
                throw new NotImplementedException();
            }
        }

        private UaValue SerializeString(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            if (variableNode.ValueRank == -1)
            {
                var jStringVal = new JValue(value.Value.ToString());
                var schema = schemaGenerator.Generate(typeof(string));
                return new UaValue(jStringVal, schema);
            }
            else
            {
                //TODO: Gestire il caso degli array
                throw new NotImplementedException();
            }
        }

        private UaValue SerializeXmlElement(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();
            //The stack is not able to handle xml elements. Bug maybe.
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
            else
            {
                //TODO: Gestire il caso degli array
                throw new NotImplementedException();
            }
        }

        private UaValue SerializeEnumeration(VariableNode variableNode, Variant value)
        {
            var schemaGenerator = new JSchemaGenerator();

            LocalizedText[] enumString;
            EnumValueType[] enumValues;

            int enstrreturn = GetEnumStrings(variableNode.DataType, out enumString, out enumValues);
            dynamic valueOut;
            if (variableNode.ValueRank == -1)
            {
                int index = (int)value.Value;
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
                                
                var schema = new JSchema
                {
                    Type = JSchemaType.Object,
                    Properties =
                    {
                        { "EnumValue", new JSchema { Type = JSchemaType.Integer } },
                        //TODO: sarebbe carino mettere Enum nel seguente schema con i valori di enumValues
                        { "EnumLabel", new JSchema { Type = JSchemaType.String  } }
                    }
                };
                return new UaValue(valueOut, schema);
            }
            else
            {
                //TODO: Gestire il caso degli array
                throw new NotImplementedException();
            }
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
    }
}