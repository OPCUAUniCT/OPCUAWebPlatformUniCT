using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Exceptions
{
    public class ValueToWriteTypeException: Exception
    {
        public ValueToWriteTypeException() : base() { }

        public ValueToWriteTypeException(String message) : base(message) { }

        public ValueToWriteTypeException(String message, Exception innerException) : base(message, innerException) { }
    }
}
