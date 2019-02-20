using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;
using ObjectHashServer.Models.Extensions;

namespace ObjectHashServer.Services.Implementations
{
    public class ObjectRedactionImplementation
    {
        public (JToken json, JToken salts) RedactJToken(JToken json, JToken redactSettings, JToken salts = null)
        {
            // deep clone JTokens which are changed (json object and salts)
            return RecursivlyRedactDataAndSalts(json.DeepClone(), redactSettings, salts.IsNullOrEmpty() ? null : salts.DeepClone());
        }

        /// <summary>
        /// Redacts a given JSON object (JToken) for the provided redaction setting. 
        /// The redact setting can be any valid JSON with objects and arrays but as 
        /// values it can ONLY contain Booleans. For each value 'true' in the redact 
        /// settings the counterpart in the JSON will be blacked out.
        /// </summary>
        private (JToken json, JToken salts) RecursivlyRedactDataAndSalts(JToken json, JToken redactSettings, JToken salts = null)
        {
            switch (redactSettings.Type)
            {
                case JTokenType.Boolean:
                    if ((bool)redactSettings)
                    {
                        ObjectHashImplementation objectHash = new ObjectHashImplementation();
                        objectHash.HashJToken(json, salts);
                        return ("**REDACTED**" + objectHash.HashAsString(), "**REDACTED**");
                    }

                    return (json, salts);
                case JTokenType.Object:
                    try
                    {
                        return RedactObject((JObject)json, (JObject)redactSettings, salts.IsNullOrEmpty() ? null : (JObject)salts);
                    }
                    catch (InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an object -> {} where the redact settings require one. Please check the JSON data or the redact settings.", e);
                    }
                case JTokenType.Array:
                    try {
                        return RedactArray((JArray)json, (JArray)redactSettings, salts.IsNullOrEmpty() ? null : (JArray)salts);
                    }
                    catch(InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an array -> [] where the redact settings require one. Please check the JSON data or the redact settings", e);
                    }
                default:
                    throw new BadRequestException("The redact setting JSON is invalid. It can only contain a nested JSON, arrays and the data type Boolean.");
            }
        }

        private (JToken json, JToken salts) RedactObject(JObject json, JObject redactSettings, JObject salts = null)
        {
            foreach (var redactSetting in redactSettings)
            {
                if (!json.ContainsKey(redactSetting.Key) || (!salts.IsNullOrEmpty() && !salts.ContainsKey(redactSetting.Key)))
                {
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                        {
                            { "missingKey", redactSetting.Key },
                            { "errorInObject", !json.ContainsKey(redactSetting.Key) ? "json" : "salts" }
                        };

                    throw new BadRequestException("The provided JSON or Salt defines an object which is different from the redact settings object. Please check the JSON, the salt data or the redact settings.", additionalExceptionData);
                }

                if(salts.IsNullOrEmpty())
                {
                    (json[redactSetting.Key], _) = RecursivlyRedactDataAndSalts(json[redactSetting.Key], redactSettings[redactSetting.Key], null);
                }
                else
                {
                    (json[redactSetting.Key], salts[redactSetting.Key]) = RecursivlyRedactDataAndSalts(json[redactSetting.Key], redactSettings[redactSetting.Key], salts[redactSetting.Key]);
                }
            }

            return (json, salts);
        }

        private (JToken json, JToken salts) RedactArray(JArray json, JArray redactSettings, JArray salts = null)
        {
            if (redactSettings.Count != json.Count || (!salts.IsNullOrEmpty() && salts.Count != json.Count))
            {
                IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        { "errorInObject", redactSettings.Count == json.Count ? "json" : "salts" }
                    };

                throw new BadRequestException("The corresponding JSON or Salt object contains an array that is different in size from the redact settings array. They need to be equaly long.", additionalExceptionData);
            }

            // for each element in the array apply the redact function
            for (int i = 0; i < redactSettings.Count; i++)
            {
                if(salts.IsNullOrEmpty())
                {
                    (json[i], _) = RecursivlyRedactDataAndSalts(json[i], redactSettings[i], null);
                }
                else
                {
                    (json[i], salts[i]) = RecursivlyRedactDataAndSalts(json[i], redactSettings[i], salts[i]);
                }
            }

            return (json, salts);
        }
    }
}
