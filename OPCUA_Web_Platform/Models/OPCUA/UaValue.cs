using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;

namespace WebPlatform.Models.OPCUA
{
    public class UaValue
    {
        public readonly JObject Value;
        public readonly JSchema Schema;

        public UaValue()
        {
        }

        public UaValue(JObject value, JSchema schema)
        {
            Value = value;
            Schema = schema;
        }
    }
}