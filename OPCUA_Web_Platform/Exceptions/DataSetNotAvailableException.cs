using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebPlatform.Exceptions
{
    public class DataSetNotAvailableException: Exception
    {
        public DataSetNotAvailableException() : base() { }

        public DataSetNotAvailableException(String message) : base(message) { }

        public DataSetNotAvailableException(String message, Exception innerException) : base(message, innerException) { }
    }
}
