using System;

namespace WebPlatform.Exceptions
{
    public class NotSupportedNamespaceException: Exception
    {
        public NotSupportedNamespaceException(String message) : base(message) { }
    }
}