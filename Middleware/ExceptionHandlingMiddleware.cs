using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (BaseException e)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = e.StatusCode;
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new { message = e.Message, statusCode = e.StatusCode }));
            }
        }
    }
}
