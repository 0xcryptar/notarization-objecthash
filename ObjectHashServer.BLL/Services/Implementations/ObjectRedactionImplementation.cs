using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;
using ObjectHashServer.BLL.Models.Extensions;
using System.Collections;

// ReSharper disable PossibleNullReferenceException

namespace ObjectHashServer.BLL.Services.Implementations
{
    public static class ObjectRedactionImplementation
    {
        /// <summary>
        /// Redacts a given JSON object (JToken) for the provided redaction setting. 
        /// The redact setting can be any valid JSON with objects and arrays but as 
        /// values it can ONLY contain Booleans. For each value 'true' in the redact 
        /// settings the counterpart in the JSON will be blacked out.
        /// Additionally a DSL to create dynamic redaction settings can be use. Please
        /// refer to the documentation.
        /// This method does change the provided inputs. So there is no need to clone
        /// them before calling this method.
        /// </summary>
        public static (JToken json, JToken salts) RedactJToken(JToken json, JToken redactSettings, JToken salts = null)
        {
            JToken evaluatedRedactSettings = EvaluateCommandsImplementation.EvaluateCommands(redactSettings, json);
            return RecursiveRedactDataAndSalts(json.DeepClone(), evaluatedRedactSettings,
                salts.IsNullOrEmpty() ? null : salts.DeepClone());
        }

        private static (JToken json, JToken salts) RecursiveRedactDataAndSalts(JToken json, JToken redactSettings,
            JToken salts = null)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (redactSettings.Type)
            {
                case JTokenType.Boolean:
                    if (!(bool)redactSettings) return (json, salts);

                    ObjectHashImplementation objectHash = new ObjectHashImplementation();
                    objectHash.HashJToken(json, salts);
                    return ("**REDACTED**" + objectHash.HashAsString(), "**REDACTED**");
                case JTokenType.Object:
                    try
                    {
                        return RedactObject((JObject)json, (JObject)redactSettings,
                            salts.IsNullOrEmpty() ? null : (JObject)salts);
                    }
                    catch (InvalidCastException e)
                    {
                        throw new BadRequestException(
                            "The provided JSON does not contain an object -> {} where the redact settings require one. Please check the JSON data or the redact settings.", e);
                    }
                case JTokenType.Array:
                    try
                    {
                        return RedactArray((JArray)json, (JArray)redactSettings,
                            salts.IsNullOrEmpty() ? null : (JArray)salts);
                    }
                    catch (InvalidCastException e)
                    {
                        throw new BadRequestException(
                            "The provided JSON does not contain an array -> [] where the redact settings require one. Please check the JSON data or the redact settings", e);
                    }
                case JTokenType.Null:
                    {
                        return (json, salts);
                    }
                default:
                    throw new BadRequestException(
                        "The redact setting JSON is invalid. It can only contain a nested JSON, arrays and the data type Boolean.");
            }
        }

        private static (JToken json, JToken salts) RedactObject(JObject json, JObject redactSettings,
            JObject salts = null)
        {
            foreach ((string key, JToken _) in redactSettings)
            {
                if (!json.ContainsKey(key) || (!salts.IsNullOrEmpty() && !salts.ContainsKey(key)))
                {
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        {"missingKey", key},
                        {"errorInObject", !json.ContainsKey(key) ? "json" : "salts"}
                    };

                    throw new BadRequestException(
                        "The provided JSON or Salt defines an object which is different from the redact settings object. Please check the JSON, the salt data or the redact settings.",
                        additionalExceptionData);
                }

                if (salts.IsNullOrEmpty())
                {
                    (json[key], _) = RecursiveRedactDataAndSalts(json[key], redactSettings[key]);
                }
                else
                {
                    (json[key], salts[key]) = RecursiveRedactDataAndSalts(json[key], redactSettings[key], salts[key]);
                }
            }

            return (json, salts);
        }

        private static (JToken json, JToken salts) RedactArray(JArray json, JArray redactSettings, JArray salts = null)
        {
            if (redactSettings.Count != json.Count || (!salts.IsNullOrEmpty() && salts.Count != json.Count))
            {
                IDictionary additionalExceptionData = new Dictionary<string, object>
                {
                    {"errorInObject", redactSettings.Count == json.Count ? "json" : "salts"}
                };

                throw new BadRequestException(
                    "The corresponding JSON or Salt object contains an array that is different in size from the redact settings array. They need to be equally long.",
                    additionalExceptionData);
            }

            for (int i = 0; i < redactSettings.Count; i++)
            {
                if (salts.IsNullOrEmpty())
                {
                    (json[i], _) = RecursiveRedactDataAndSalts(json[i], redactSettings[i]);
                }
                else
                {
                    (json[i], salts[i]) = RecursiveRedactDataAndSalts(json[i], redactSettings[i], salts[i]);
                }
            }

            return (json, salts);
        }
    }
}