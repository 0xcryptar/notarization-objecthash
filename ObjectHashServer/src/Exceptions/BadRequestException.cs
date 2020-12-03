using System;
using System.Collections;

namespace ObjectHashServer.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException() : base("Bad Request (400)") { }
        public BadRequestException(string message) : base(message) { }
        public BadRequestException(string message, Exception exception) : base(message, exception) { }
        public BadRequestException(string message, IEnumerable dictionary) : base(message, dictionary) { }
        public BadRequestException(string message, IEnumerable dictionary, Exception exception) : base(message, dictionary, exception) { }
    }
}
