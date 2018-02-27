using System;
using Newtonsoft.Json.Schema;

namespace WebPlatform.OPCUALayer
{
    public static class DataTypeSchemaGenerator
    {
        public static JSchema GenerateSchemaForArray(int[] dimensions, JSchema innermostSchema)
        {
            var dimLen = dimensions.Length;

            if (dimLen == 1)
            {
                var schema = new JSchema
                {
                    Type = JSchemaType.Array,
                    Items = { innermostSchema },
                    MinimumItems = dimensions[0],
                    MaximumItems = dimensions[0] 
                };
                
                return schema;
            } 
            
            JSchema innerSchema = new JSchema();
            JSchema outerSchema = new JSchema();
            for (int dim = dimLen-1; dim >= 0; dim--)
            {
                if (dim == dimLen-1)
                {
                    innerSchema = new JSchema
                    {
                        Type = JSchemaType.Array,
                        Items = { innermostSchema },
                        MinimumItems = dimensions[dim],
                        MaximumItems = dimensions[dim]
                    };
                }
                else
                {
                    outerSchema = new JSchema
                    {
                        Type = JSchemaType.Array,
                        Items = { innerSchema },
                        MinimumItems = dimensions[dim],
                        MaximumItems = dimensions[dim]
                    };
                    innerSchema = outerSchema;
                }
            }

            return outerSchema;
        }
    }
}