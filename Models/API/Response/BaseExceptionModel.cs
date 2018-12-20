using System;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Models.API.Response
{
    public class BaseExceptionModel
    {
        // TODO: check if needed
        public BaseExceptionModel(BaseException baseException)
        {
            Message = baseException.Message;
            StatusCode = baseException.StatusCode;
        }

        public string Message { get; set; }
        public int StatusCode { get; set; }
    }
}
