using System;

namespace ObjectHashServer.Exceptions
{
    public class BadRequestException : Exception
    {
        public BadRequestException(string message, Exception innerException = null)
           : base(message, innerException)
        { }
    }
}
