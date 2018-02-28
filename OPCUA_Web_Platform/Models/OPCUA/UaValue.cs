using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace WebPlatform.Models.OPCUA
{
    public class UaValue
    {
        public readonly JToken Value;
        public readonly JSchema Schema;

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