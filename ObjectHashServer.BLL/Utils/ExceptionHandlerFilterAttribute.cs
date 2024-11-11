using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ObjectHashServer.BLL.Exceptions;
using ObjectHashServer.BLL.Models.Api.Response;

namespace ObjectHashServer.BLL.Utils
{
    /// <inheritdoc />
    /// <summary>
    /// Exception handling middleware. This code is based on:
    /// https://github.com/bitwarden/server/blob/86aa342bad71c90a076aae6ca7f254eb5d6b8c7d/src/Api/Utilities/ExceptionHandlerFilterAttribute.cs
    /// </summary>
    public class ExceptionHandlerFilterAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            ErrorResponseModel errorModel = new ErrorResponseModel("An error has occurred.");
            bool logException = false;
            Exception exception = context.Exception;

            switch (exception)
            {
                case null:
                    // Should never happen, early out
                    return;
                case BadRequestException _:
                    logException = true;

                    context.HttpContext.Response.StatusCode = 400;
                    errorModel.Message = exception.Message;
                    break;
                case NotSupportedException _ when !string.IsNullOrWhiteSpace(exception.Message):
                    errorModel.Message = exception.Message;
                    context.HttpContext.Response.StatusCode = 400;
                    break;
                case ApplicationException _:
                    context.HttpContext.Response.StatusCode = 402;
                    break;
                case NotFoundException _:
                    errorModel.Message = "Resource not found.";
                    context.HttpContext.Response.StatusCode = 404;
                    break;
                case UnauthorizedAccessException _:
                    errorModel.Message = "Unauthorized.";
                    context.HttpContext.Response.StatusCode = 401;
                    break;
                default:
                    // log all non standard exceptions
                    logException = true;

                    errorModel.Message = "An unhandled server error has occurred.";
                    context.HttpContext.Response.StatusCode = 500;
                    break;
            }

            if (logException)
            {
                ILogger logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<ExceptionHandlerFilterAttribute>>();
                logger.LogError(0, exception, exception.Message);
            }

            IHostEnvironment env = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                errorModel.ExceptionMessage = exception.Message;
                errorModel.ExceptionStackTrace = exception.StackTrace;
                errorModel.InnerExceptionMessage = exception.InnerException?.Message;
            }

            context.Result = new ObjectResult(errorModel);
        }
    }
}
