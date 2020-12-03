using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;

namespace ObjectHashServer.Models.Api.Request
{
    public class ObjectRedactionRequestModel : ObjectBaseRequestModel
    {
        [Required]
        public JToken RedactSettings { get; set; }
    }
}

