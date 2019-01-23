using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.API.Response;

namespace ObjectHashServer.Utils
{
    /// <summary>
    /// Exception handling middleware. This is based on the great Bitwarden repo:
    /// https://github.com/bitwarden/server/blob/86aa342bad71c90a076aae6ca7f254eb5d6b8c7d/src/Api/Utilities/ExceptionHandlerFilterAttribute.cs
    /// </summary>
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            ErrorResponseModel errorModel = new ErrorResponseModel("An error has occurred.");
            bool logException = false;
            Exception exception = context.Exception;

            if (exception == null)
            {
                // Should never happen
                return;
            }

            if (exception is BadRequestException)
            {
                logException = true;

                context.HttpContext.Response.StatusCode = 400;
                errorModel.Message = exception.Message;
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
                // log all non standard exceptions
                logException = true;

                errorModel.Message = "An unhandled server error has occurred.";
                context.HttpContext.Response.StatusCode = 500;
            }

            if (logException)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionHandlerFilterAttribute>>();
                logger.LogError(0, exception, exception.Message);
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
