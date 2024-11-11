using System;
using System.IO;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.OpenApi.Models;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.Functions.Worker.Http;


using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Api.Response;
using ObjectHashServer.BLL.Services.Implementations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace ObjectHashServer.API
{
    public class ObjectHasherFunctions
    {
        /// <summary>
        /// Generates Salts for the recieved ObjectBaseRequestModel.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [Function("HashObject")]
        [OpenApiOperation(operationId: "hash-object", Description = "Generates salts for the recieved json.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(JObject), Description = "Json for which the salts should be generated.", Required = true)]
        [OpenApiParameter(name: "generateSalts", In = ParameterLocation.Query, Required = true, Type = typeof(bool), Description = "Generate salts?")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ObjectHashResponseModel), Description = "The generated/hashed result for the given json.")]
        public async Task<IActionResult> HashObject([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "hash-object")] HttpRequest req)
        {
            try
            {
                // check if the salts should be generated or not
                // if the query does not disable salt generation, by default salts are generated
                bool generateSalts;
                if (!bool.TryParse(req.Query["generateSalts"], out generateSalts))
                    generateSalts = true;

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

                if (!generateSalts)
                    requestModel.Salts = null;

                return new OkObjectResult(new ObjectHashResponseModel(new ObjectHash(requestModel)));
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
        [Function("ReHashObject")]
        [OpenApiOperation(operationId: "rehash-object", Description = "Generates the hash for the recieved ObjectBaseRequestModel.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ObjectBaseRequestModel), Description = "ObjectBaseRequestModel for which the hash should be generated.", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ObjectHashResponseModel), Description = "The generated ObjectHashResponseModel containing the generated hash.")]
        public async Task<IActionResult> ReHashObject([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "rehash-object")] HttpRequest req)
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

                return new OkObjectResult(new ObjectHashResponseModel(new ObjectHash(requestModel)));
            }
            catch (Exception e)
            {
                var result = new ObjectResult(e);
                result.StatusCode = StatusCodes.Status500InternalServerError;
                return result;
            }
        }

        /// <summary>
        /// Calculates the redacted JSON.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [Function("RedactObject")]
        [OpenApiOperation(operationId: "redact-object", Description = "Calculates the redacted JSON given a settings file.")]
        [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ObjectRedactionRequestModel), Description = "ObjectRedactionRequestModel for which the redacted JSON should be calculated.", Required = true)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(ObjectRedactionResponseModel), Description = "ObjectRedactionResponseModel containing the redacted JSON.")]
        public async Task<IActionResult> RedactObject([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "redact-object")] HttpRequest req)
        {
            try
            {
                ObjectRedactionRequestModel requestModel = null;

                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    requestModel = JsonConvert.DeserializeObject<ObjectRedactionRequestModel>(requestBody);
                }
                catch (Exception e)
                {
                    var result = new ObjectResult(e);
                    result.StatusCode = StatusCodes.Status400BadRequest;
                    return result;
                }

                ObjectRedactionImplementation.RedactJToken(requestModel.Data, requestModel.RedactSettings, requestModel.Salts);
                return new OkObjectResult(new ObjectRedactionResponseModel(new ObjectRedaction(requestModel)));
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