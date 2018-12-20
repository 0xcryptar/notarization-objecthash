using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ObjectHashServer.Exceptions
{
    public abstract class BaseException : Exception
    {
        public int StatusCode { get; set; }
    }
}
