using Microsoft.AspNetCore.Mvc;
using ObjectHashServer.Models;
using ObjectHashServer.Models.Api.Request;
using ObjectHashServer.Models.Api.Response;
using ObjectHashServer.Services.Implementations;

namespace ObjectHashServer.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ObjectHashController : ControllerBase
    {
        [HttpPost]
        public ActionResult<ObjectHashResponseModel> Post([FromBody]ObjectHashRequestModel model, [FromQuery]bool generateSalts)
        {
            if(generateSalts)
            {
                GenerateSaltsImplementation.SaltsForObjectBaseRequestModel(model);
            } 
            // TODO: the return changes the pure data object
            // eg. if the object contains a float value of 1.0 
            // it is returned as integer
            return new ObjectHashResponseModel(new ObjectHash(model));
        }
    }
}
