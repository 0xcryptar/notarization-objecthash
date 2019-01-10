using System;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public class JsonRedactionImplementation
    {
        /// <summary>
        /// Redacts the json for a given redaction setting. The redact setting can
        /// be a Json with objects and arrays but as values it can ONLY contain
        /// Booleans. For each value 'true' in the redact settings the corresponding
        /// Json object will be blackout out.
        /// </summary>
        /// <returns>A JToken which is redacted on specified places</returns>
        /// <param name="json">The original Json object</param>
        /// <param name="redactSettings">The redact setting for redacting the Json object</param>
        public JToken RedactJson(JToken json, JToken redactSettings)
        {
            JToken jsonClone = json.DeepClone();
            // only work on the deep cloned data
            return RecursivlyRedactJson(jsonClone, redactSettings);
        }

        private JToken RecursivlyRedactJson(JToken json, JToken redactSettings)
        {
            switch (redactSettings.Type)
            {
                case JTokenType.Boolean:
                    if ((bool)redactSettings)
                    {
                        ObjectHashImplementation h = new ObjectHashImplementation();
                        h.HashAny(json);
                        return "**REDACTED**" + h.HashAsString();
                    }

                    return json;
                case JTokenType.Object:
                    try
                    {
                        return RedactObject((JObject)json, (JObject)redactSettings);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("The provided JSON does not contain an object -> {} where the readact settings require one. Please check the JSON data or the redact settings.");
                    }
                case JTokenType.Array:
                    try {
                        return RedactArray((JArray)json, (JArray)redactSettings);
                    }
                    catch(InvalidCastException)
                    {
                        throw new BadRequestException("The provided JSON does not contain an array -> [] where the readact settings require one. Please check the JSON data or the redact settings");
                    }
                default:
                    throw new BadRequestException("The redact setting JSON is invalid. It can only contain a nested JSON, arrays and the data type Boolean.");
            }
        }

        private JToken RedactObject(JObject json, JObject redactSettings)
        {
            foreach (var redactSetting in redactSettings)
            {
                // check if Json object has same keys as the redact settings
                if (!json.ContainsKey(redactSetting.Key))
                {
                    throw new BadRequestException("The provided JSON has an object which is different from the object defined in the readact settings. Please check the JSON data or the redact settings.");
                }

                json[redactSetting.Key] = RecursivlyRedactJson(json[redactSetting.Key], redactSettings[redactSetting.Key]);
            }

            return json;
        }

        private JToken RedactArray(JArray json, JArray redactSettings)
        {
            // check if the arrays have same size
            if (redactSettings.Count != json.Count)
            {
                throw new BadRequestException("The corresponding JSON object contains an array that is different in size from the redact settings array. They need to be equaly long.");
            }

            // for each element in the array apply the redact function
            for (int i = 0; i < redactSettings.Count; i++)
            {
                json[i] = RecursivlyRedactJson(json[i], redactSettings[i]);
            }

            return json;
        }
    }
}
