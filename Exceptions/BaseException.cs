using System;
using System.Collections;

namespace ObjectHashServer.Exceptions
{
    public class BaseException : Exception
    {
        public BaseException(string message, Exception innerException = null)
           : base(message, innerException)
        {
            if(innerException != null)
                base.Data.Add("innerException", innerException);
        }

        public BaseException(string message, IDictionary additionalData)
            : base(message)
        {
            base.Data.Add("additionalData", additionalData);
        }

        public BaseException(string message, IDictionary additionalData, Exception innerException = null)
            : base(message, innerException)
        {
            base.Data.Add("additionalData", additionalData);

            if (innerException != null)
                base.Data.Add("innerException", innerException);
        }
    }
}
