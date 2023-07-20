using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Threading.Tasks;
using System;
using System.Net.Http;

namespace ObjectHashServer.API
{
    public class SwaggerFunctions
    {
        private readonly string NotarizationAPIURL = "https://notarization-objecthash.azurewebsites.net/api";

        private readonly HttpClient client = new HttpClient(new HttpClientHandler());

        /// <summary>
        /// Enable the access of the OAuth2-Redirect endpoint for the gateway..
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [FunctionName("Gateway-OAuth2-Redirect")]
        [OpenApiOperation(operationId: "gateway-oauth2-redirect", Description = "Enable the access of the OAuth2-Redirect endpoint for the gateway.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/html", bodyType: typeof(string), Description = "The oauth2-redirect.html file.")]
        public async Task<HttpResponseMessage> ReturnOuth2RedirectHtml([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gateway-oauth2-redirect.html")] HttpRequestMessage req)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(NotarizationAPIURL + "/oauth2-redirect.html");
                var ret = req.CreateResponse(HttpStatusCode.OK);
                ret.Content = response.Content;
                return ret;
            }
            catch (Exception e)
            {
                var result = new ObjectResult(e);
                result.StatusCode = StatusCodes.Status500InternalServerError;
                return null;
            }
        }

        /// <summary>
        /// Generates swagger for gateway.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [FunctionName("GatewayOpenapi")]
        [OpenApiOperation(operationId: "gateway-oauth2-redirect", Description = "Enable the access of the OAuth2-Redirect endpoint for the gateway.")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/html", bodyType: typeof(string), Description = "The oauth2-redirect.html file.")]
        public async Task<ActionResult<string>> GenerateGatewayOpenapiSwagger([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gateway-openapi/{version}.{extension}")] HttpRequest req,
            string version, string extension)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(NotarizationAPIURL + "/openapi/" + version + "." + extension);
                string json = await response.Content.ReadAsStringAsync();

                JObject swagger = JObject.Parse(json);
                JArray servers = (JArray) swagger.SelectToken("servers");
                servers.Clear();
                JObject url = new JObject();
                url.Add("url", "api.cryptar.de");
                servers.Add(url);
                return swagger.ToString(Formatting.Indented);
            }
            catch (Exception e)
            {
                var result = new ObjectResult(e);
                result.StatusCode = StatusCodes.Status500InternalServerError;
                return result;
            }
        }

        /// <summary>
        /// Generates swagger for gateway.
        /// </summary>
        /// <returns/>
        /// <response code="200">Successful call</response>
        /// <response code="404">Not Found</response>
        /// <response code="500">Internal Server Error</response>
        [FunctionName("GatewaySwagger")]
        public async Task<ActionResult<string>> GenerateGatewaySwagger([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "gateway-swagger.{extension}")] HttpRequest req, 
            string extension)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(NotarizationAPIURL + "/swagger." + extension);
                string json = await response.Content.ReadAsStringAsync();

                JObject swagger = JObject.Parse(json);
                swagger["host"] = "api.cryptar.de";
                swagger["basePath"] = "/JSON2hash";
                return swagger.ToString(Formatting.Indented);
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
