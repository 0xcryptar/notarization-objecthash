using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.Models;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Models.Api.Response;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ObjectRedactionController : ControllerBase
    {
        [HttpPost]
        public ActionResult<ObjectRedactionResponseModel> Post([FromBody]ObjectRedactionRequestModel model)
        {
            return new ObjectRedactionResponseModel(new ObjectRedaction(model));
        }
    }
}
