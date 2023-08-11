using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.BLL.Models.Api.Request
{
    [OpenApiExample(typeof(ObjectRedactionRequestModelExample))]
    public class ObjectRedactionRequestModel : ObjectBaseRequestModel
    {
        [Required]
        public JToken RedactSettings { get; set; }
    }
}
