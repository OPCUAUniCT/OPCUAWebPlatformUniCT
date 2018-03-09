using System;
using Opc.Ua;
using Opc.Ua.Client;

namespace WebPlatform.OPCUALayer
{
    public class DataTypeAnalyzer
    {
        private Session m_session;
        
        public DataTypeAnalyzer(Session session)
        {
            this.m_session = session;
        }

        public static BuiltInType GetBuiltinTypeFromStandardTypeDescription(string type)
        {
            switch (type)
            {
                case "Bit": case "Boolean":
                    return BuiltInType.Boolean;
                case "SByte":
                    return BuiltInType.SByte;
                case "Byte":
                    return BuiltInType.Byte;
                case "Int16":
                    return BuiltInType.Int16;
                case "UInt16":
                    return BuiltInType.UInt16;
                case "Int32":
                    return BuiltInType.Int32;
                case "UInt32":
                    return BuiltInType.UInt32;
                case "Int64":
                    return BuiltInType.Int64;
                case "UInt64":
                    return BuiltInType.UInt64;
                case "Float":
                    return BuiltInType.Float;
                case "Double":
                    return BuiltInType.Double;
                case "Char": case "WideChar": 
                case "String": case "CharArray":
                case "WideString": case "WideCharArray":
                    return BuiltInType.String;
                case "DateTime":
                    return BuiltInType.DateTime;
                case "ByteString":
                    return BuiltInType.ByteString;
                case "Guid":
                    return BuiltInType.Guid;
                default:
                    return BuiltInType.Null;
            }
        }
        
        internal NodeId GetDataTypeEncodingNodeId(NodeId dataTypeNodeId)
        {
            ReferenceDescriptionCollection refDescriptionCollection;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                dataTypeNodeId,
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HasEncoding,
                true,
                (uint)NodeClass.Object,
                out continuationPoint,
                out refDescriptionCollection);

            //Choose always first encoding
            return (NodeId)refDescriptionCollection[0].NodeId;
        }
        
        internal NodeId GetDataTypeDescriptionNodeId(NodeId dataTypeEncodingNodeId)
        {
            ReferenceDescriptionCollection refDescriptionCollection;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                dataTypeEncodingNodeId, //starting node is always an EncodingNode
                0u,
                BrowseDirection.Forward,
                ReferenceTypeIds.HasDescription,  //HasDescription reference
                true,
                (uint)NodeClass.Variable,
                out continuationPoint,
                out refDescriptionCollection);

            return (NodeId)refDescriptionCollection[0].NodeId;
        }
        
        internal string GetDictionary(NodeId dataTypeDescriptionNodeId)
        {
            ReferenceDescriptionCollection refDescriptionCollection;
            byte[] continuationPoint;

            m_session.Browse(
                null,
                null,
                dataTypeDescriptionNodeId, //the starting node is a DataTypeDescription
                0u,
                BrowseDirection.Inverse, //It is an inverse Reference 
                ReferenceTypeIds.HasComponent, //So it is ComponentOf
                true,
                (uint)NodeClass.Variable,
                out continuationPoint,
                out refDescriptionCollection);

            var dataTypeDictionaryNodeId = (NodeId)refDescriptionCollection[0].NodeId;

            var dataValueCollection = Read(dataTypeDictionaryNodeId, Attributes.Value);

            return System.Text.Encoding.UTF8.GetString((byte[])dataValueCollection[0].Value);
        }
        
        private DataValueCollection Read(NodeId nodeId, uint attributeId)
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