using System.Collections.Generic;

namespace ObjectHashServer.BLL.Models.Api.Response
{
    public class ErrorResponseModel : ResponseModel
    {
        public ErrorResponseModel() : base("error")
        { }

        public ErrorResponseModel(string message) : this()
        {
            Message = message;
        }

        public ErrorResponseModel(Dictionary<string, IEnumerable<string>> errors)
            : this("Errors have occurred.", errors)
        { }

        public ErrorResponseModel(string errorKey, string errorValue)
            : this(errorKey, new string[] { errorValue })
        { }

        public ErrorResponseModel(string errorKey, IEnumerable<string> errorValues)
            : this(new Dictionary<string, IEnumerable<string>> { { errorKey, errorValues } })
        { }

        public ErrorResponseModel(string message, Dictionary<string, IEnumerable<string>> errors)
            : this()
        {
            Message = message;
            ValidationErrors = errors;
        }

        public string Message { get; set; }
        public Dictionary<string, IEnumerable<string>> ValidationErrors { get; set; }
        // For use in development environments.
        public string ExceptionMessage { get; set; }
        public string ExceptionStackTrace { get; set; }
        public string InnerExceptionMessage { get; set; }
    }
}
