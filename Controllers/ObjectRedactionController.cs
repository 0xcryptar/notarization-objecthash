using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.Models;
using ObjectHashServer.Models.API.Request;
using ObjectHashServer.Models.API.Response;

namespace ObjectHashServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ObjectRedactionController : ControllerBase
    {
        [HttpPost]
        public ActionResult<ObjectRedactionResponseModel> Post([FromBody]ObjectRedactionRequestModel model)
        {
            ObjectRedaction objectRedaction = new ObjectRedaction(model);
            return new ObjectRedactionResponseModel(objectRedaction);
        }
    }
}
