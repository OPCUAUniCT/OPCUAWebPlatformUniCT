using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Opc.Ua;
using Opc.Ua.Client;

namespace WebPlatform.Models.OPCUA
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
                        break;
                    case BuiltInType.SByte: case BuiltInType.Byte:
                    case BuiltInType.Int16: case BuiltInType.UInt16:
                    case BuiltInType.Int32: case BuiltInType.UInt32:
                    case BuiltInType.Int64: case BuiltInType.UInt64:
                        break;
                    case BuiltInType.Float:
                        break;
                    case BuiltInType.Double:
                        break;
                    case BuiltInType.String:         case BuiltInType.DateTime:      case BuiltInType.Guid:
                    case BuiltInType.DiagnosticInfo: case BuiltInType.NodeId:        case BuiltInType.ExpandedNodeId:
                    case BuiltInType.StatusCode:     case BuiltInType.QualifiedName: case BuiltInType.LocalizedText:
                        break;
                    case BuiltInType.XmlElement:
                        break;
                    case BuiltInType.ByteString:
                        break;
                    case BuiltInType.Enumeration:
                        break;
                    case BuiltInType.ExtensionObject:
                        break;
            }
        }
    }
}