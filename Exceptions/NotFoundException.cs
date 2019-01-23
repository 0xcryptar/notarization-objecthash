using System;
using System.Collections;

namespace ObjectHashServer.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException() : base("Resource not found (404)") { }
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception exception) : base(message, exception) { }
        public NotFoundException(string message, IDictionary dictonary) : base(message, dictonary) { }
        public NotFoundException(string message, IDictionary dictonary, Exception exception) : base(message, dictonary, exception) { }
    }
}
