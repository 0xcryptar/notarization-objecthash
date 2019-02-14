using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public class ObjectRedactionImplementation
    {
        public (JToken json, JToken salts) RedactJToken(JToken json, JToken redactSettings, JToken salts = null)
        {
            // deep clone JTokens which are changed
            return RecursivlyRedactDataAndSalts(json.DeepClone(), redactSettings, salts?.DeepClone());
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
                        switch(json.Type)
                        {
                            case JTokenType.Array:
                                return RedactArrayWithCommand((JArray)json, (JObject)redactSettings, salts);
                            default:
                                return RedactObject((JObject)json, (JObject)redactSettings, salts);
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an object -> {} where the redact settings require one. Please check the JSON data or the redact settings.", e);
                    }
                case JTokenType.Array:
                    try {
                        return RedactArray((JArray)json, (JArray)redactSettings, salts);
                    }
                    catch(InvalidCastException e)
                    {
                        throw new BadRequestException("The provided JSON does not contain an array -> [] where the redact settings require one. Please check the JSON data or the redact settings", e);
                    }
                default:
                    throw new BadRequestException("The redact setting JSON is invalid. It can only contain a nested JSON, arrays and the data type Boolean.");
            }
        }

        private (JToken json, JToken salts) RedactObject(JObject json, JObject redactSettings, JToken salts = null)
        {
            foreach (var redactSetting in redactSettings)
            {
                // TODO: salt check if not null

                // check if JSON object has same keys as the redact settings
                if (!json.ContainsKey(redactSetting.Key))
                {
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        { "missingKey", redactSetting.Key }
                    };

                    throw new BadRequestException("The provided JSON has an object which is different from the object defined in the redact settings. Please check the JSON data or the redact settings.", additionalExceptionData);
                }

                (json[redactSetting.Key], salts[redactSetting.Key]) = RecursivlyRedactDataAndSalts(json[redactSetting.Key], redactSettings[redactSetting.Key], salts?[redactSetting.Key]);
            }

            return (json, salts);
        }

        private (JToken json, JToken salts) RedactArray(JArray json, JArray redactSettings, JToken salts = null)
        {
            // TODO: if salt not null check amount is the same

            // check if the arrays have same size
            if (redactSettings.Count != json.Count)
            {
                throw new BadRequestException("The corresponding JSON object contains an array that is different in size from the redact settings array. They need to be equaly long.");
            }

            // for each element in the array apply the redact function
            for (int i = 0; i < redactSettings.Count; i++)
            {
                (json[i], salts[i]) = RecursivlyRedactDataAndSalts(json[i], redactSettings[i], salts?[i]);
            }

            return (json, salts);
        }

        // the DSL for redacting an array with a command like forEach
        // is still an early feature and needs more development
        private (JToken json, JToken salts) RedactArrayWithCommand(JArray json, JObject command, JToken salts = null)
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
                return RedactArrayForEach(json, command["REDACT:forEach"], salts);
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

        private (JToken json, JToken salts) RedactArrayForEach(JArray json, JToken redactSettings, JToken salts = null)
        {
            for (int i = 0; i < json.Count; i++)
            {
                (json[i], salts[i]) = RecursivlyRedactDataAndSalts(json[i], redactSettings, salts?[i]);
            }

            return (json, salts);
        }
    }
}
