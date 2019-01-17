using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.API.Response;

namespace ObjectHashServer.Utils
{
    /// <summary>
    /// Exception handling middleware. This is based on the great Bitwarden repo
    /// 
    /// </summary>
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            var errorModel = new ErrorResponseModel("An error has occurred.");

            var exception = context.Exception;
            if (exception == null)
            {
                // Should never happen.
                return;
            }

            var badRequestException = exception as BadRequestException;
            if (badRequestException != null)
            {
                context.HttpContext.Response.StatusCode = 400;
                errorModel.Message = badRequestException.Message;
            }
            else if (exception is NotSupportedException && !string.IsNullOrWhiteSpace(exception.Message))
            {
                errorModel.Message = exception.Message;
                context.HttpContext.Response.StatusCode = 400;
            }
            else if (exception is ApplicationException)
            {
                context.HttpContext.Response.StatusCode = 402;
            }
            else if (exception is NotFoundException)
            {
                errorModel.Message = "Resource not found.";
                context.HttpContext.Response.StatusCode = 404;
            }
            else if (exception is UnauthorizedAccessException)
            {
                errorModel.Message = "Unauthorized.";
                context.HttpContext.Response.StatusCode = 401;
            }
            else
            {
                // add sentry error logging

                errorModel.Message = "An unhandled server error has occurred.";
                context.HttpContext.Response.StatusCode = 500;
            }

            var env = context.HttpContext.RequestServices.GetRequiredService<IHostingEnvironment>();
            if (env.IsDevelopment())
            {
                errorModel.ExceptionMessage = exception.Message;
                errorModel.ExceptionStackTrace = exception.StackTrace;
                errorModel.InnerExceptionMessage = exception?.InnerException?.Message;
            }

            context.Result = new ObjectResult(errorModel);
        }
    }
}
