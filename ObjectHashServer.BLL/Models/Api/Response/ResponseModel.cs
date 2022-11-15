using Newtonsoft.Json;

namespace ObjectHashServer.BLL.Models.Api.Response
{
    public abstract class ResponseModel
    {
        protected ResponseModel(string obj)
        {
            if (string.IsNullOrWhiteSpace(obj))
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Object = obj;
        }

        [JsonIgnore]
        public string Object { get; private set; }
    }
}
