using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Api.Response;
using ObjectHashServer.BLL.Services.Implementations;

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

