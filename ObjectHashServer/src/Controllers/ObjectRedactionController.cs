using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Api.Response;

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
