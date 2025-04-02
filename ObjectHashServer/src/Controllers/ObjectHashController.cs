using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.BLL.Models;
using ObjectHashServer.BLL.Models.Api.Request;
using ObjectHashServer.BLL.Models.Api.Response;
using ObjectHashServer.BLL.Services.Implementations;

namespace ObjectHashServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ObjectHashController : ControllerBase
    {
        [HttpPost]
        public ActionResult<ObjectHashResponseModel> Post([FromBody] ObjectBaseRequestModel model, [FromQuery] bool generateSalts)
        {
            if (generateSalts)
            {
                GenerateSaltsImplementation.SetRandomSaltsForObjectBaseRequestModel(model);
            }
            return new ObjectHashResponseModel(new ObjectHash(model));
        }
    }
}
