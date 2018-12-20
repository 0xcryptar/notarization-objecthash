using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ObjectHashServer.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException(string message) {
            this.StatusCode = StatusCodes.Status400BadRequest;
            // this.Message = message;
        } //: this(string.Empty, message) { }

        // TODO: check
        /*
        public BadRequestException(string key, string errorMessage)
            : base("BadRequestException")
        {
            ModelState = new ModelStateDictionary();
            ModelState.AddModelError(key, errorMessage);
        }

        public BadRequestException(ModelStateDictionary modelState)
            : base("BadRequestException")
        {
            if (modelState.IsValid || modelState.ErrorCount == 0)
            {
                return;
            }

            ModelState = modelState;
        } */
    }
}
