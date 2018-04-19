using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Models.DataSet
{
    public class VariableState
    {
        public JToken Value { get; set; }
        public bool IsValid => Value != null;
        
        
    }
}
