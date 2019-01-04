using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public class JsonRedactionImplementation
    {
        public JObject RedactJson(JObject json, JObject redactSettings)
        {
            JObject jsonClone = (JObject)json.DeepClone();
            // only work on the deep cloned data
            return RecursivlyRedactJson(jsonClone, redactSettings);
        }

        private JObject RecursivlyRedactJson(JObject json, JObject redactSettings)
        {
            foreach (var x in redactSettings)
            {
                // check if key of redact settings is also present in JSON
                if(!json.ContainsKey(x.Key))
                {
                    throw new BadRequestException("The presented redactSetting does not match the data JSON object.");
                }

                JToken value = x.Value;
                switch (value.Type)
                {
                    case JTokenType.Boolean:
                        if (value.ToObject<bool>())
                        {
                            ObjectHashImplementation h = new ObjectHashImplementation();
                            h.HashAny(json[x.Key]);
                            json[x.Key] = "**REDACTED**" + h.ToHex();
                        }

                        break;
                    case JTokenType.Object:
                        RecursivlyRedactJson((JObject)json[x.Key], (JObject)x.Value);
                        break;
                    case JTokenType.Array:
                        JArray jsonArray;

                        try
                        {
                            jsonArray = (JArray)json[x.Key];
                        } catch(Exception) // TODO: check the Exception type and only catch the subset
                        {
                            throw new BadRequestException("The corresponding JSON object is not an array, but the redactSetting requiers one.");
                        }
                  
                        // check if the arrays have the same size
                        if (jsonArray.Count != ((JArray)value).Count) {
                            throw new BadRequestException("The corresponding JSON object has an array that is different in size from the redactSetting. They need to be equal.");
                        }

                        // for each element in the array apply the redact function
                        for (int i = 0; i < jsonArray.Count; i++)
                        {
                            try
                            {
                                if ((bool)((JArray)value)[i])
                                {
                                    ObjectHashImplementation h = new ObjectHashImplementation();
                                    h.HashAny(jsonArray[i]);
                                    jsonArray[i] = "**REDACTED**" + h.ToHex();
                                }
                            } catch (FormatException)
                            {
                                throw new BadRequestException("The redactSetting JSON is invalid. It can only contain a nested JSON and the data type Boolean.");
                            }
                        }
                        break;
                    default:
                        throw new BadRequestException("The redactSetting JSON is invalid. It can only contain a nested JSON and the data type Boolean.");
                }
            }
            return json;
        }
    }
}
