using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public class JsonRedactionImplementation
    {
        /// <summary>
        /// Redacts a given JSON object (JToken) for the provided redaction setting. 
        /// The redact setting can be any valid JSON with objects and arrays but as 
        /// values it can ONLY contain Booleans. For each value 'true' in the redact 
        /// settings the counterpart in the JSON will be blacked out.
        /// </summary>
        /// <returns>JToken which is redacted on specified postions via the readact settings</returns>
        /// <param name="json">The original JSON object</param>
        /// <param name="redactSettings">The redact setting for redacting the JSON object</param>
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
                        h.HashJToken(json);
                        return "**REDACTED**" + h.HashAsString();
                    }

                    return json;
                case JTokenType.Object:
                    try
                    {
                        switch(json.Type)
                        {
                            case JTokenType.Array:
                                return RedactArrayWithCommand((JArray)json, (JObject)redactSettings);
                            default:
                                return RedactObject((JObject)json, (JObject)redactSettings);
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an object -> {} where the redact settings require one. Please check the JSON data or the redact settings.", e);
                    }
                case JTokenType.Array:
                    try {
                        return RedactArray((JArray)json, (JArray)redactSettings);
                    }
                    catch(InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an array -> [] where the redact settings require one. Please check the JSON data or the redact settings", e);
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
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        { "missingKey", redactSetting.Key }
                    };

                    throw new BadRequestException("The provided JSON has an object which is different from the object defined in the redact settings. Please check the JSON data or the redact settings.", additionalExceptionData);
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

        // TODO: improve. The DSL for redacting an array with a command like forEach
        // is still an early feature and needs more development
        private JToken RedactArrayWithCommand(JArray json, JObject command)
        {
            // check that command is object with single command only
            if(command.Count != 1)
            {
                IDictionary additionalExceptionData = new Dictionary<string, object>
                {
                    { "commandObject", command }
                };

                throw new BadRequestException("A command object can only contain one command element. Please read the manual or contact an admin", additionalExceptionData);
            }

            if(command.ContainsKey("REDACT:forEach"))
            {
                return RedactArrayForEach(json, command["REDACT:forEach"]);
            }
            // TODO: add new commands
            // else if() { }
            else
            {
                IDictionary additionalExceptionData = new Dictionary<string, object>
                {
                    { "commandObject", command }
                };

                throw new BadRequestException("You tried to use a redact command. The command you used is not valid. Currently available: 'REDACT:forEach'", additionalExceptionData);
            }
        }

        private JToken RedactArrayForEach(JArray json, JToken redactSettings)
        {
            for (int i = 0; i < json.Count; i++)
            {
                json[i] = RecursivlyRedactJson(json[i], redactSettings);
            }

            return json;
        }
    }
}
