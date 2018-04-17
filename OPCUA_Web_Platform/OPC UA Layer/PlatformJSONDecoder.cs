using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Newtonsoft.Json;
using Opc.Ua;
using WebPlatform.Exceptions;
using TypeInfo = Opc.Ua.TypeInfo;

namespace WebPlatform.OPC_UA_Layer
{
    public class PlatformJSONDecoder:  IDecoder
    {
        #region Private Fields
        private JsonTextReader m_reader;
        private Dictionary<string, object> m_root;
        private Stack<object> m_stack;
        private ServiceMessageContext m_context;
        private ushort[] m_namespaceMappings;
        private ushort[] m_serverMappings;
        private uint m_nestingLevel;
        #endregion

        #region Constructors
        public PlatformJSONDecoder(string json, ServiceMessageContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            Initialize();

            m_context = context;
            m_nestingLevel = 0;
            m_reader = new JsonTextReader(new StringReader(json));
            m_root = ReadObject();
            m_stack = new Stack<object>();
            m_stack.Push(m_root);
        }

        /// <summary>
        /// Sets private members to default values.
        /// </summary>
        private void Initialize()
        {
            m_reader = null;
        }
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Initializes the tables used to map namespace and server uris during decoding.
        /// </summary>
        /// <param name="namespaceUris">The namespaces URIs referenced by the data being decoded.</param>
        /// <param name="serverUris">The server URIs referenced by the data being decoded.</param>
        public void SetMappingTables(NamespaceTable namespaceUris, StringTable serverUris)
        {
            m_namespaceMappings = null;

            if (namespaceUris != null && m_context.NamespaceUris != null)
            {
                m_namespaceMappings = m_context.NamespaceUris.CreateMapping(namespaceUris, false);
            }

            m_serverMappings = null;

            if (serverUris != null && m_context.ServerUris != null)
            {
                m_serverMappings = m_context.ServerUris.CreateMapping(serverUris, false);
            }
        }

        /// <summary>
        /// Closes the stream used for reading.
        /// </summary>
        public void Close()
        {
            m_reader.Close();
        }

        /// <summary>
        /// Closes the stream used for reading.
        /// </summary>
        public void Close(bool checkEof)
        {
            if (checkEof && m_reader.TokenType != JsonToken.EndObject)
            {
                while (m_reader.Read() && m_reader.TokenType != JsonToken.EndObject) ;
            }

            m_reader.Close();
        }

        private List<object> ReadArray()
        {
            List<object> elements = new List<object>();

            while (m_reader.Read() && m_reader.TokenType != JsonToken.EndArray)
            {
                switch (m_reader.TokenType)
                {
                    case JsonToken.Comment:
                        {
                            break;
                        }

                    case JsonToken.Boolean:
                    case JsonToken.Integer:
                    case JsonToken.Float:
                    case JsonToken.String:
                        {
                            elements.Add(m_reader.Value);
                            break;
                        }

                    case JsonToken.StartArray:
                        {
                            elements.Add(ReadArray());
                            break;
                        }

                    case JsonToken.StartObject:
                        {
                            elements.Add(ReadObject());
                            break;
                        }
                }
            }

            return elements;
        }

        private Dictionary<string, object> ReadObject()
        {
            Dictionary<string, object> fields = new Dictionary<string, object>();

            while (m_reader.Read() && m_reader.TokenType != JsonToken.EndObject)
            {
                if (m_reader.TokenType == JsonToken.PropertyName)
                {
                    string name = (string)m_reader.Value;

                    if (m_reader.Read() && m_reader.TokenType != JsonToken.EndObject)
                    {
                        switch (m_reader.TokenType)
                        {
                            case JsonToken.Comment:
                                {
                                    break;
                                }

                            case JsonToken.Null:
                            case JsonToken.Date:
                                {
                                    fields[name] = m_reader.Value;
                                    break;
                                }

                            case JsonToken.Bytes:
                            case JsonToken.Boolean:
                            case JsonToken.Integer:
                            case JsonToken.Float:
                            case JsonToken.String:
                                {
                                    fields[name] = m_reader.Value;
                                    break;
                                }

                            case JsonToken.StartArray:
                                {
                                    fields[name] = ReadArray();
                                    break;
                                }

                            case JsonToken.StartObject:
                                {
                                    fields[name] = ReadObject();
                                    break;
                                }
                        }
                    }
                }
            }

            return fields;
        }

        /// <summary>
        /// Reads the body extension object from the stream.
        /// </summary>
        public object ReadExtensionObjectBody(ExpandedNodeId typeId)
        {
            return null;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Frees any unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// An overrideable version of the Dispose.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_reader != null)
                {
                    m_reader.Close();
                }
            }
        }
        #endregion

        #region IDecoder Members
        /// <summary>
        /// The type of encoding being used.
        /// </summary>
        public EncodingType EncodingType
        {
            get { return EncodingType.Json; }
        }

        /// <summary>
        /// The message context associated with the decoder.
        /// </summary>
        public ServiceMessageContext Context
        {
            get { return m_context; }
        }

        /// <summary>
        /// Pushes a namespace onto the namespace stack.
        /// </summary>
        public void PushNamespace(string namespaceUri)
        {
        }

        /// <summary>
        /// Pops a namespace from the namespace stack.
        /// </summary>
        public void PopNamespace()
        {
        }

        public bool ReadField(string fieldName, out object token)
        {
            token = null;

            if (String.IsNullOrEmpty(fieldName))
            {
                token = m_stack.Peek();
                return true;
            }

            var context = m_stack.Peek() as Dictionary<string, object>;

            if (context == null || !context.TryGetValue(fieldName, out token))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads a boolean from the stream.
        /// </summary>
        public bool ReadBoolean(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as bool?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a Boolean as expected");
            }

            return (bool)token;
        }

        /// <summary>
        /// Reads a sbyte from the stream.
        /// </summary>
        public sbyte ReadSByte(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }

            if (value < SByte.MinValue || value > SByte.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (sbyte)value;
        }

        /// <summary>
        /// Reads a byte from the stream.
        /// </summary>
        public byte ReadByte(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a Integer as expected");
            }

            if (value < Byte.MinValue || value > Byte.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (byte)value;
        }

        /// <summary>
        /// Reads a short from the stream.
        /// </summary>
        public short ReadInt16(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }
;
            if (value < Int16.MinValue || value > Int16.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (short)value;
        }

        /// <summary>
        /// Reads a ushort from the stream.
        /// </summary>
        public ushort ReadUInt16(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }

            if (value < UInt16.MinValue || value > UInt16.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (ushort)value;
        }

        /// <summary>
        /// Reads an int from the stream.
        /// </summary>
        public int ReadInt32(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");

            }

            if (value < Int32.MinValue || value > Int32.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (int)value;
        }

        /// <summary>
        /// Reads a uint from the stream.
        /// </summary>
        public uint ReadUInt32(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }

            if (value < UInt32.MinValue || value > UInt32.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (uint)value;
        }

        /// <summary>
        /// Reads a long from the stream.
        /// </summary>
        public long ReadInt64(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }

            return (long)value;
        }

        /// <summary>
        /// Reads a ulong from the stream.
        /// </summary>
        public ulong ReadUInt64(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as long?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Integer as expected");
            }

            if (value < 0)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (ulong)value;
        }

        /// <summary>
        /// Reads a float from the stream.
        /// </summary>
        public float ReadFloat(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as double?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a Number as expected");
            }

            if (value < Single.MinValue || value > Single.MaxValue)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return (float)value;
        }

        /// <summary>
        /// Reads a double from the stream.
        /// </summary>
        public double ReadDouble(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as double?;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a Number as expected");
            }

            return (double)value;
        }

        /// <summary>
        /// Reads a string from the stream.
        /// </summary>
        public string ReadString(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as string;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a String as expected");
            }

            if (m_context.MaxStringLength > 0 && m_context.MaxStringLength < value.Length)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return value;
        }

        /// <summary>
        /// Reads a UTC date/time from the stream.
        /// </summary>
        public DateTime ReadDateTime(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as DateTime?;

            if (value != null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a date string as expected");
            }

            return (DateTime)value;
        }

        /// <summary>
        /// Reads a GUID from the stream.
        /// </summary>
        public Uuid ReadGuid(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as string;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a string as expected");
            }

            try
            {
                return new Uuid(value);
            }
            catch (FormatException exc)
            {
                throw new ValueToWriteTypeException("String not formatted correctly. " + exc.Message);
            }
        }

        /// <summary>
        /// Reads a byte string from the stream.
        /// </summary>
        public byte[] ReadByteString(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as string;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a String as expected");
            }

            var bytes = Convert.FromBase64String(value);

            if (m_context.MaxByteStringLength > 0 && m_context.MaxByteStringLength < bytes.Length)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is out of range");
            }

            return bytes;
        }

        /// <summary>
        /// Reads an XmlElement from the stream.
        /// </summary>
        public XmlElement ReadXmlElement(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                return null;
            }

            var value = token as string;

            if (value == null)
            {
                return null;
            }

            var bytes = Convert.FromBase64String(value);

            if (bytes != null && bytes.Length > 0)
            {
                XmlDocument document = new XmlDocument();
                document.InnerXml = new UTF8Encoding().GetString(bytes);
                return document.DocumentElement;
            }

            return null;
        }

        /// <summary>
        /// Reads an NodeId from the stream.
        /// </summary>
        public NodeId ReadNodeId(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as string;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not a String as expected");
            }

            return ParsePlatformNodeIdString(value);
        }
        
        /// <summary>
        /// Reads an ExpandedNodeId from the stream.
        /// </summary>
        public ExpandedNodeId ReadExpandedNodeId(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            string nodeId = null;
            string namespaceUri = null;
            uint serverIndex = 0;

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Object as expected");
            }

            try
            {
                m_stack.Push(value);

                if (!value.ContainsKey("NodeId") || !value.ContainsKey("NamespaceUri") || !value.ContainsKey("ServerIndex"))
                {
                    throw new ValueToWriteTypeException("Error: Property named " + fieldName + " must have the properties NodeId, NamespaceUri and ServerIndex");
                }

                nodeId = ReadString("NodeId");
                namespaceUri = ReadString("NamespaceUri");
                serverIndex = ReadUInt32("ServerIndex");
               
            }
            finally
            {
                m_stack.Pop();
            }

            return new ExpandedNodeId(nodeId, namespaceUri, serverIndex);
            
        }

        /// <summary>
        /// Reads an StatusCode from the stream.
        /// </summary>
        public StatusCode ReadStatusCode(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            string stringCode = null;
            uint uintCode = 0;
            
            
            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Object as expected");
            }

            try
            {
                m_stack.Push(value);

                if (!value.ContainsKey("Code") || !value.ContainsKey("StructureChanged"))
                {
                    throw new ValueToWriteTypeException("Error: Property named " + fieldName + " must have the properties Code and StructureChanged");
                }

                stringCode = ReadString("NodeId");
                // Get a PropertyInfo of specific property type(T).GetProperty(....)
                PropertyInfo propertyInfo = typeof(StatusCodes).GetProperty(stringCode, BindingFlags.Public | BindingFlags.Static); 
                if (propertyInfo == null)
                    throw new ValueToWriteTypeException("The string in code is not a valid value");
                object codeValue = propertyInfo.GetValue(null, null);

                uintCode = (uint) codeValue;
                
            }
            finally
            {
                m_stack.Pop();
            }
            
            return new StatusCode(uintCode);

        }

        /// <summary>
        /// Reads an DiagnosticInfo from the stream.
        /// </summary>
        public DiagnosticInfo ReadDiagnosticInfo(string fieldName)
        {
            object token = (object) null;
            if (!ReadField(fieldName, out token))
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            Dictionary<string, object> dictionary = token as Dictionary<string, object>;
            if (dictionary == null)
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an object as expected");
            try
            {
                m_stack.Push(dictionary);
                
                if (!dictionary.ContainsKey("SymbolicId") || !dictionary.ContainsKey("NamespaceUri") || 
                    !dictionary.ContainsKey("Locale") || !dictionary.ContainsKey("LocalizedText") || 
                    !dictionary.ContainsKey("AdditionalInfo") || !dictionary.ContainsKey("InnerStatusCode") || 
                    !dictionary.ContainsKey("InnerDiagnosticInfo"))
                {
                    throw new ValueToWriteTypeException("Error: Property named " + fieldName + " must have the properties SymbolicId, NamespaceUri, Locale, LocalizedText, AdditionaInfo," +
                                                        "InnerStatusCode and InnerDiagnosticInfo ");
                }
                
                var diagnosticInfo = new DiagnosticInfo();
                diagnosticInfo.SymbolicId = ReadInt32("SymbolicId");
                diagnosticInfo.NamespaceUri = ReadInt32("NamespaceUri");
                diagnosticInfo.Locale = ReadInt32("Locale");
                diagnosticInfo.LocalizedText = ReadInt32("LocalizedText");
                diagnosticInfo.AdditionalInfo = ReadString("AdditionalInfo");
                diagnosticInfo.InnerStatusCode = ReadStatusCode("InnerStatusCode");
                diagnosticInfo.InnerDiagnosticInfo = ReadDiagnosticInfo("InnerDiagnosticInfo");
                return diagnosticInfo;
            }
            finally
            {
                m_stack.Pop();
            }
        }

        /// <summary>
        /// Reads an QualifiedName from the stream.
        /// </summary>
        public QualifiedName ReadQualifiedName(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Object as expected");
            }
            
            string name = null;
            ushort namespaceIndex = 0;
            
            try
            {
                m_stack.Push(value);

                if (!value.ContainsKey("Name") || !value.ContainsKey("NamespaceIndex"))
                {
                    throw new ValueToWriteTypeException("Error: Property named " + fieldName + " must have the properties Name and NamespaceIndex");
                }

                name = ReadString("Name");
                namespaceIndex = ReadUInt16("NamespaceIndex");
               
            }
            finally
            {
                m_stack.Pop();
            }

            return new QualifiedName(name, namespaceIndex);
        }

        /// <summary>
        /// Reads an LocalizedText from the stream.
        /// </summary>
        public LocalizedText ReadLocalizedText(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " missing");
            }

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                throw new ValueToWriteTypeException("Error: Property named " + fieldName + " is not an Object as expected");
            }
            
            string locale = null;
            string text = null;
            
            try
            {
                m_stack.Push(value);

                if (!value.ContainsKey("Locale") || !value.ContainsKey("Text"))
                {
                    throw new ValueToWriteTypeException("Error: Property named " + fieldName + " must have the properties Locale, and Text");
                }

                locale = ReadString("Locale");
                text = ReadString("Text");
               
            }
            finally
            {
                m_stack.Pop();
            }

            return new LocalizedText(locale, text);
        }

        private Variant ReadVariantBody(string fieldName, BuiltInType type)
        {
            switch (type)
            {
                case BuiltInType.Boolean: { return new Variant(ReadBoolean(fieldName), TypeInfo.Scalars.Boolean); }
                case BuiltInType.SByte: { return new Variant(ReadSByte(fieldName), TypeInfo.Scalars.SByte); }
                case BuiltInType.Byte: { return new Variant(ReadByte(fieldName), TypeInfo.Scalars.Byte); }
                case BuiltInType.Int16: { return new Variant(ReadInt16(fieldName), TypeInfo.Scalars.Int16); }
                case BuiltInType.UInt16: { return new Variant(ReadUInt16(fieldName), TypeInfo.Scalars.UInt16); }
                case BuiltInType.Int32: { return new Variant(ReadInt32(fieldName), TypeInfo.Scalars.Int32); }
                case BuiltInType.UInt32: { return new Variant(ReadUInt32(fieldName), TypeInfo.Scalars.UInt32); }
                case BuiltInType.Int64: { return new Variant(ReadInt64(fieldName), TypeInfo.Scalars.Int64); }
                case BuiltInType.UInt64: { return new Variant(ReadUInt64(fieldName), TypeInfo.Scalars.UInt64); }
                case BuiltInType.Float: { return new Variant(ReadFloat(fieldName), TypeInfo.Scalars.Float); }
                case BuiltInType.Double: { return new Variant(ReadDouble(fieldName), TypeInfo.Scalars.Double); }
                case BuiltInType.String: { return new Variant(ReadString(fieldName), TypeInfo.Scalars.String); }
                case BuiltInType.ByteString: { return new Variant(ReadByteString(fieldName), TypeInfo.Scalars.ByteString); }
                case BuiltInType.DateTime: { return new Variant(ReadDateTime(fieldName), TypeInfo.Scalars.DateTime); }
                case BuiltInType.Guid: { return new Variant(ReadGuid(fieldName), TypeInfo.Scalars.Guid); }
                case BuiltInType.NodeId: { return new Variant(ReadNodeId(fieldName), TypeInfo.Scalars.NodeId); }
                case BuiltInType.ExpandedNodeId: { return new Variant(ReadExpandedNodeId(fieldName), TypeInfo.Scalars.ExpandedNodeId); }
                case BuiltInType.QualifiedName: { return new Variant(ReadQualifiedName(fieldName), TypeInfo.Scalars.QualifiedName); }
                case BuiltInType.LocalizedText: { return new Variant(ReadLocalizedText(fieldName), TypeInfo.Scalars.LocalizedText); }
                case BuiltInType.StatusCode: { return new Variant(ReadStatusCode(fieldName), TypeInfo.Scalars.StatusCode); }
                case BuiltInType.XmlElement: { return new Variant(ReadXmlElement(fieldName), TypeInfo.Scalars.XmlElement); }
                case BuiltInType.ExtensionObject: { return new Variant(ReadExtensionObject(fieldName), TypeInfo.Scalars.ExtensionObject); }
                case BuiltInType.Variant: { return new Variant(ReadVariant(fieldName), TypeInfo.Scalars.Variant); }
            }

            return Variant.Null;
        }

        private Variant ReadVariantArrayBody(string fieldName, BuiltInType type)
        {
            switch (type)
            {
                case BuiltInType.Boolean: { return new Variant(ReadBooleanArray(fieldName), TypeInfo.Arrays.Boolean); }
                case BuiltInType.SByte: { return new Variant(ReadSByteArray(fieldName), TypeInfo.Arrays.SByte); }
                case BuiltInType.Byte: { return new Variant(ReadByteArray(fieldName), TypeInfo.Arrays.Byte); }
                case BuiltInType.Int16: { return new Variant(ReadInt16Array(fieldName), TypeInfo.Arrays.Int16); }
                case BuiltInType.UInt16: { return new Variant(ReadUInt16Array(fieldName), TypeInfo.Arrays.UInt16); }
                case BuiltInType.Int32: { return new Variant(ReadInt32Array(fieldName), TypeInfo.Arrays.Int32); }
                case BuiltInType.UInt32: { return new Variant(ReadUInt32Array(fieldName), TypeInfo.Arrays.UInt32); }
                case BuiltInType.Int64: { return new Variant(ReadInt64Array(fieldName), TypeInfo.Arrays.Int64); }
                case BuiltInType.UInt64: { return new Variant(ReadUInt64Array(fieldName), TypeInfo.Arrays.UInt64); }
                case BuiltInType.Float: { return new Variant(ReadFloatArray(fieldName), TypeInfo.Arrays.Float); }
                case BuiltInType.Double: { return new Variant(ReadDoubleArray(fieldName), TypeInfo.Arrays.Double); }
                case BuiltInType.String: { return new Variant(ReadStringArray(fieldName), TypeInfo.Arrays.String); }
                case BuiltInType.ByteString: { return new Variant(ReadByteStringArray(fieldName), TypeInfo.Arrays.ByteString); }
                case BuiltInType.DateTime: { return new Variant(ReadDateTimeArray(fieldName), TypeInfo.Arrays.DateTime); }
                case BuiltInType.Guid: { return new Variant(ReadGuidArray(fieldName), TypeInfo.Arrays.Guid); }
                case BuiltInType.NodeId: { return new Variant(ReadNodeIdArray(fieldName), TypeInfo.Arrays.NodeId); }
                case BuiltInType.ExpandedNodeId: { return new Variant(ReadExpandedNodeIdArray(fieldName), TypeInfo.Arrays.ExpandedNodeId); }
                case BuiltInType.QualifiedName: { return new Variant(ReadQualifiedNameArray(fieldName), TypeInfo.Arrays.QualifiedName); }
                case BuiltInType.LocalizedText: { return new Variant(ReadLocalizedTextArray(fieldName), TypeInfo.Arrays.LocalizedText); }
                case BuiltInType.StatusCode: { return new Variant(ReadStatusCodeArray(fieldName), TypeInfo.Arrays.StatusCode); }
                case BuiltInType.XmlElement: { return new Variant(ReadXmlElementArray(fieldName), TypeInfo.Arrays.XmlElement); }
                case BuiltInType.ExtensionObject: { return new Variant(ReadExtensionObjectArray(fieldName), TypeInfo.Arrays.ExtensionObject); }
                case BuiltInType.Variant: { return new Variant(ReadVariantArray(fieldName), TypeInfo.Arrays.Variant); }
            }

            return Variant.Null;
        }

        /// <summary>
        /// Reads an Variant from the stream.
        /// </summary>
        public Variant ReadVariant(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                return Variant.Null;
            }

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                return Variant.Null;
            }

            // check the nesting level for avoiding a stack overflow.
            if (m_nestingLevel > m_context.MaxEncodingNestingLevels) 
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    m_context.MaxEncodingNestingLevels);
            }
            try {
                m_nestingLevel++;
                m_stack.Push(value);

                BuiltInType type = (BuiltInType)ReadByte("Type");

                var context = m_stack.Peek() as Dictionary<string, object>;

                if (!context.TryGetValue("Body", out token))
                {
                    return Variant.Null;
                }

                if (token is Array)
                {
                    var array = ReadVariantBody("Body", type);
                    var dimensions = ReadInt32Array("Dimensions");

                    if (array.Value is Array && dimensions != null && dimensions.Count > 1)
                    {
                        array = new Variant(new Matrix((Array)array.Value, type, dimensions.ToArray()));
                    }

                    return array;
                }
                else
                {
                    return ReadVariantBody("Body", type);
                }
            }
            finally
            {
                m_nestingLevel--;
                m_stack.Pop();
            }
        }

        /// <summary>
        /// Reads an DataValue from the stream.
        /// </summary>
        public DataValue ReadDataValue(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                return null;
            }

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                return null;
            }

            DataValue dv = new DataValue();

            try
            {
                m_stack.Push(value);

                dv.WrappedValue = ReadVariant("Value");
                dv.StatusCode = ReadStatusCode("StatusCode");
                dv.SourceTimestamp = ReadDateTime("SourceTimestamp");
                dv.SourcePicoseconds = ReadUInt16("SourcePicoseconds");
                dv.ServerTimestamp = ReadDateTime("ServerTimestamp");
                dv.ServerPicoseconds = ReadUInt16("ServerPicoseconds");
            }
            finally
            {
                m_stack.Pop();
            }

            return dv;
        }

        private void EncodeAsJson(JsonTextWriter writer, object value)
        {
            var map = value as Dictionary<string, object>;

            if (map != null)
            {
                EncodeAsJson(writer, map);
                return;
            }

            var list = value as List<object>;

            if (list != null)
            {
                writer.WriteStartArray();

                foreach (var element in list)
                {
                    EncodeAsJson(writer, element);
                }

                writer.WriteStartArray();
                return;
            }

            writer.WriteValue(value);
        }

        private void EncodeAsJson(JsonTextWriter writer, Dictionary<string, object> value)
        {
            writer.WriteStartObject();

            foreach (var field in value)
            {
                writer.WritePropertyName(field.Key);
                EncodeAsJson(writer, field.Value);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Reads an extension object from the stream.
        /// </summary>
        public ExtensionObject ReadExtensionObject(string fieldName)
        {
            object token = null;

            if (!ReadField(fieldName, out token))
            {
                return null;
            }

            var value = token as Dictionary<string, object>;

            if (value == null)
            {
                return null;
            }

            try
            {
                m_stack.Push(value);

                NodeId typeId = ReadNodeId("TypeId");

                ExpandedNodeId absoluteId = NodeId.ToExpandedNodeId(typeId, m_context.NamespaceUris);

                if (!NodeId.IsNull(typeId) && NodeId.IsNull(absoluteId))
                {
                    Utils.Trace("Cannot de-serialized extension objects if the NamespaceUri is not in the NamespaceTable: Type = {0}", typeId);
                }

                byte encoding = ReadByte("Encoding");

                if (encoding == 1)
                {
                    var bytes = ReadByteString("Body");
                    return new ExtensionObject(typeId, bytes);
                }

                if (encoding == 2)
                {
                    var xml = ReadXmlElement("Body");
                    return new ExtensionObject(typeId, xml);
                }

                Type systemType = m_context.Factory.GetSystemType(typeId);

                if (systemType != null)
                {
                    var encodeable = ReadEncodeable("Body", systemType);
                    return new ExtensionObject(typeId, encodeable);
                }

                var ostrm = new MemoryStream();

                using (JsonTextWriter writer = new JsonTextWriter(new StreamWriter(ostrm)))
                {
                    EncodeAsJson(writer, token);
                }

                return new ExtensionObject(typeId, ostrm.ToArray());
            }
            finally
            {
                m_stack.Pop();
            }
        }

        /// <summary>
        /// Reads an encodeable object from the stream.
        /// </summary>
        public IEncodeable ReadEncodeable(
            string fieldName,
            System.Type systemType)
        {
            if (systemType == null) throw new ArgumentNullException("systemType");

            object token = null;

            if (!ReadField(fieldName, out token))
            {
                return null;
            }

            IEncodeable value = Activator.CreateInstance(systemType) as IEncodeable;

            if (value == null)
            {
                throw new ServiceResultException(StatusCodes.BadDecodingError, Utils.Format("Type does not support IEncodeable interface: '{0}'", systemType.FullName));
            }

            // check the nesting level for avoiding a stack overflow.
            if (m_nestingLevel > m_context.MaxEncodingNestingLevels)
            {
                throw ServiceResultException.Create(
                    StatusCodes.BadEncodingLimitsExceeded,
                    "Maximum nesting level of {0} was exceeded",
                    m_context.MaxEncodingNestingLevels);
            }

            m_nestingLevel++;

            try
            {
                m_stack.Push(token);

                value.Decode(this);
            }
            finally
            {
                m_stack.Pop();
            }

            m_nestingLevel--;

            return value;
        }

        /// <summary>
        ///  Reads an enumerated value from the stream.
        /// </summary>
        public Enum ReadEnumerated(string fieldName, System.Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");

            return (Enum)Enum.ToObject(enumType, ReadInt32(fieldName));
        }

        private bool ReadArrayField(string fieldName, out List<object> array)
        {
            object token = array = null;

            if (!ReadField(fieldName, out token))
            {
                return false;
            }

            array = token as List<object>;

            if (array == null)
            {
                return false;
            }

            if (m_context.MaxArrayLength > 0 && m_context.MaxArrayLength < array.Count)
            {
                throw new ServiceResultException(StatusCodes.BadEncodingLimitsExceeded);
            }

            return true;
        }

        /// <summary>
        /// Reads a boolean array from the stream.
        /// </summary>
        public BooleanCollection ReadBooleanArray(string fieldName)
        {
            var values = new BooleanCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadBoolean(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a sbyte array from the stream.
        /// </summary>
        public SByteCollection ReadSByteArray(string fieldName)
        {
            var values = new SByteCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadSByte(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a byte array from the stream.
        /// </summary>
        public ByteCollection ReadByteArray(string fieldName)
        {
            var values = new ByteCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadByte(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a short array from the stream.
        /// </summary>
        public Int16Collection ReadInt16Array(string fieldName)
        {
            var values = new Int16Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadInt16(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a ushort array from the stream.
        /// </summary>
        public UInt16Collection ReadUInt16Array(string fieldName)
        {
            var values = new UInt16Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadUInt16(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a int array from the stream.
        /// </summary>
        public Int32Collection ReadInt32Array(string fieldName)
        {
            var values = new Int32Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadInt32(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a uint array from the stream.
        /// </summary>
        public UInt32Collection ReadUInt32Array(string fieldName)
        {
            var values = new UInt32Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadUInt32(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a long array from the stream.
        /// </summary>
        public Int64Collection ReadInt64Array(string fieldName)
        {
            var values = new Int64Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadInt64(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a ulong array from the stream.
        /// </summary>
        public UInt64Collection ReadUInt64Array(string fieldName)
        {
            var values = new UInt64Collection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadUInt64(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a float array from the stream.
        /// </summary>
        public FloatCollection ReadFloatArray(string fieldName)
        {
            var values = new FloatCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadFloat(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a double array from the stream.
        /// </summary>
        public DoubleCollection ReadDoubleArray(string fieldName)
        {
            var values = new DoubleCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadDouble(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a string array from the stream.
        /// </summary>
        public StringCollection ReadStringArray(string fieldName)
        {
            var values = new StringCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadString(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a UTC date/time array from the stream.
        /// </summary>
        public DateTimeCollection ReadDateTimeArray(string fieldName)
        {
            var values = new DateTimeCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    values.Add(ReadDateTime(null));
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a GUID array from the stream.
        /// </summary>
        public UuidCollection ReadGuidArray(string fieldName)
        {
            var values = new UuidCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadGuid(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads a byte string array from the stream.
        /// </summary>
        public ByteStringCollection ReadByteStringArray(string fieldName)
        {
            var values = new ByteStringCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadByteString(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an XmlElement array from the stream.
        /// </summary>
        public XmlElementCollection ReadXmlElementArray(string fieldName)
        {
            var values = new XmlElementCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadXmlElement(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an NodeId array from the stream.
        /// </summary>
        public NodeIdCollection ReadNodeIdArray(string fieldName)
        {
            var values = new NodeIdCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadNodeId(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an ExpandedNodeId array from the stream.
        /// </summary>
        public ExpandedNodeIdCollection ReadExpandedNodeIdArray(string fieldName)
        {
            var values = new ExpandedNodeIdCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadExpandedNodeId(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an StatusCode array from the stream.
        /// </summary>
        public StatusCodeCollection ReadStatusCodeArray(string fieldName)
        {
            var values = new StatusCodeCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadStatusCode(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an DiagnosticInfo array from the stream.
        /// </summary>
        public DiagnosticInfoCollection ReadDiagnosticInfoArray(string fieldName)
        {
            var values = new DiagnosticInfoCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadDiagnosticInfo(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an QualifiedName array from the stream.
        /// </summary>
        public QualifiedNameCollection ReadQualifiedNameArray(string fieldName)
        {
            var values = new QualifiedNameCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadQualifiedName(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an LocalizedText array from the stream.
        /// </summary>
        public LocalizedTextCollection ReadLocalizedTextArray(string fieldName)
        {
            var values = new LocalizedTextCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadLocalizedText(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an Variant array from the stream.
        /// </summary>
        public VariantCollection ReadVariantArray(string fieldName)
        {
            var values = new VariantCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadVariant(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an DataValue array from the stream.
        /// </summary>
        public DataValueCollection ReadDataValueArray(string fieldName)
        {
            var values = new DataValueCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadDataValue(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an array of extension objects from the stream.
        /// </summary>
        public ExtensionObjectCollection ReadExtensionObjectArray(string fieldName)
        {
            var values = new ExtensionObjectCollection();

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return values;
            }

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadExtensionObject(null);
                    values.Add(element);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an encodeable object array from the stream.
        /// </summary>
        public Array ReadEncodeableArray(string fieldName, System.Type systemType)
        {
            if (systemType == null) throw new ArgumentNullException("systemType");

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return Array.CreateInstance(systemType, 0);
            }

            var values = Array.CreateInstance(systemType, token.Count);

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadEncodeable(null, systemType);
                    values.SetValue(element, ii);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }

        /// <summary>
        /// Reads an enumerated value array from the stream.
        /// </summary>
        public Array ReadEnumeratedArray(string fieldName, System.Type enumType)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");

            List<object> token = null;

            if (!ReadArrayField(fieldName, out token))
            {
                return Array.CreateInstance(enumType, 0);
            }

            var values = Array.CreateInstance(enumType, token.Count);

            for (int ii = 0; ii < token.Count; ii++)
            {
                try
                {
                    m_stack.Push(token[ii]);
                    var element = ReadEnumerated(null, enumType);
                    values.SetValue(element, ii);
                }
                finally
                {
                    m_stack.Pop();
                }
            }

            return values;
        }
        #endregion

        #region Private Methods
        
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
}