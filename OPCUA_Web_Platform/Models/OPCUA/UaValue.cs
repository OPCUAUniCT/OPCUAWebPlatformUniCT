using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Opc.Ua;

namespace WebPlatform.Models.OPCUA
{
    public class UaValue
    {
        public readonly JToken Value;
        public readonly JSchema Schema;

        public StatusCode? StatusCode { get; set; }

        public UaValue()
        {
        }

        public UaValue(JToken value, JSchema schema)
        {
            Value = value;
            Schema = schema;
        }
    }
}