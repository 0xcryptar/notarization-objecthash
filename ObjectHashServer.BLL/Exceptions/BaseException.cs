using System.Collections;

namespace ObjectHashServer.BLL.Exceptions
{
    public class BaseException : Exception
    {
        protected BaseException(string message, Exception innerException = null)
           : base(message, innerException)
        {
            if(innerException != null)
                base.Data.Add("innerException", innerException);
        }

        protected BaseException(string message, IEnumerable additionalData)
            : base(message)
        {
            base.Data.Add("additionalData", additionalData);
        }

        protected BaseException(string message, IEnumerable additionalData, Exception innerException = null)
            : base(message, innerException)
        {
            base.Data.Add("additionalData", additionalData);

            if (innerException != null)
                base.Data.Add("innerException", innerException);
        }
    }
}
