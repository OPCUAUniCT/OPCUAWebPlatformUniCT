using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Models.DataSet
{
    public class VariableState
    {
        public string Value { get; set; }
        public bool isValid
        {
            get { return Value != null; }
        }
    }
}
