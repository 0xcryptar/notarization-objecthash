using System;
using System.Collections;

namespace ObjectHashServer.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException() : base("Bad Request (400)") { }
        public BadRequestException(string message) : base(message) { }
        public BadRequestException(string message, Exception exception) : base(message, exception) { }
        public BadRequestException(string message, IDictionary dictonary) : base(message, dictonary) { }
        public BadRequestException(string message, IDictionary dictonary, Exception exception) : base(message, dictonary, exception) { }
    }
}
