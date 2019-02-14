using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.Models;
using ObjectHashServer.Models.API.Request;
using ObjectHashServer.Models.API.Response;

namespace ObjectHashServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ObjectHashController : ControllerBase
    {
        // TODO: check if GET with body is more natural REST
        [HttpPost]
        public ActionResult<ObjectHashResponseModel> Post([FromBody]ObjectHashRequestModel model)
        {
            ObjectHash objectHash = new ObjectHash(model);
            return new ObjectHashResponseModel(objectHash);
        }
    }
}
