using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Api.Response;
using ObjectHashServer.BLL.Services.Implementations;
using ObjectHashServer.Controllers;

namespace ObjectHashServerr.API
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> log)
        {
            _logger = log;
        }

        [FunctionName("Function1")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        //[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "sign")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        /// <summary>
        /// Generates Salts for the recieved ObjectBaseRequestModel.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [FunctionName("HashObject")]
        [OpenApiOperation(operationId: "hash-object", Description = "Generates salts for the recieved json.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(JObject), Description = "Json for which the salts should be generated.", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ObjectHashResponseModel), Description = "The generated/hashed result for the given json.")]
        public async Task<ActionResult<ObjectHashResponseModel>> HashObject([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hash-object")] HttpRequest req)
        {
            try
            {
                ObjectHashRequestModel requestModel = null;
                JObject jsonObject = null;

                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    jsonObject = JObject.Parse(requestBody);
                    requestModel = new ObjectHashRequestModel() { Data = jsonObject, Salts = null };
                }
                catch (Exception e)
                {
                    var result = new ObjectResult(e);
                    result.StatusCode = StatusCodes.Status400BadRequest;
                    return result;
                }

                GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(requestModel);
                return new ObjectHashResponseModel(new ObjectHash(requestModel));
            }
            catch (Exception e)
            {
                var result = new ObjectResult(e);
                result.StatusCode = StatusCodes.Status500InternalServerError;
                return result;
            }
        }

        /// <summary>
        /// Generates hash for the recieved json.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [FunctionName("ReHashObject")]
        [OpenApiOperation(operationId: "rehash-object", Description = "Generates the hash for the recieved ObjectBaseRequestModel.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ObjectBaseRequestModel), Description = "ObjectBaseRequestModel for which the hash should be generated.", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ObjectHashResponseModel), Description = "The generated ObjectHashResponseModel containing the generated hash.")]
        public async Task<ActionResult<ObjectHashResponseModel>> ReHashObject([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rehash-object")] HttpRequest req)
        {
            try
            {
                ObjectHashRequestModel requestModel = null;

                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    requestModel = JsonConvert.DeserializeObject<ObjectHashRequestModel>(requestBody);
                }
                catch (Exception e)
                {
                    var result = new ObjectResult(e);
                    result.StatusCode = StatusCodes.Status400BadRequest;
                    return result;
                }

                return new ObjectHashResponseModel(new ObjectHash(requestModel));
            }
            catch (Exception e)
            {
                var result = new ObjectResult(e);
                result.StatusCode = StatusCodes.Status500InternalServerError;
                return result;
            }
        }
    }
}

