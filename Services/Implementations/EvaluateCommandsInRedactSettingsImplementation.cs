using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{

    public static class EvaluateCommandsInRedactSettingsImplementation
    {
        /// <summary>
        /// If the redact settings contain command objects they will be evaluated here.
        /// Result is a redact setting containing only booleans.
        /// This method does change the provided inputs. So there is no need to clone
        /// them before calling this method.
        /// Not deep fct only only provided level!!!
        /// </summary>
        /// <returns>The fully evaluated redact settings object so that all JSON leaves only contain a boolean</returns>
        public static JToken EvaluateCommandsInRedactSettings(JToken redactSettings, JToken json)
        {
            return IsCommand(redactSettings) ? RecursiveEvaluateCommandsInRedactSettings((JObject)redactSettings.DeepClone(), json.DeepClone()) : redactSettings;
        }

        private static bool IsCommand(JToken redactSettings)
        {
            // check for a valid command
            // a valid REDACT DSL command is an JObject with exactly one element where the key starts with "REDACT"
            if (redactSettings.Type != JTokenType.Object) return false;
            List<string> objectKeys = ((JObject)redactSettings).Properties().Select(p => p.Name).ToList();
            return objectKeys.Count != 1 || !objectKeys[0].StartsWith("REDACT", Globals.STRING_COMPARE_METHOD);
        }

        private static JToken RecursiveEvaluateCommandsInRedactSettings(JObject redactSettings, JToken json)
        {
            if (!IsCommand(redactSettings))
            {
                return redactSettings;
            }
            
            List<string> objectKeys = redactSettings.Properties().Select(p => p.Name).ToList();
            switch (objectKeys[0])
            {
                case "REDACT:forEach":
                    try
                    {
                        return GenerateArrayWithForEachCommand((JObject)redactSettings["REDACT:forEach"], (JArray)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply an forEach command. But the corresponding JSON was not an array.");
                    }
                case "REDACT:ifObjectContains":
                    try
                    {
                        return RedactIfObjectContains(redactSettings["REDACT:ifObjectContains"], (JObject)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the ifObjectContains function. But the corresponding JSON is not an object.");
                    }
                case "REDACT:or":
                    try
                    {
                        return RedactOr((JArray)redactSettings["REDACT:or"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the or-Redact function. Please add the or commands as an array.");
                    }
                case "REDACT:and":
                    try
                    {
                        return RedactAnd((JArray)redactSettings["REDACT:and"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the and-Redact function. Please add the and commands as an array.");
                    }
                default:
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        { "commandObject", redactSettings }
                    };

                    throw new BadRequestException("You have tried to use a redact command. The command you used is not valid. Currently available: 'REDACT:forEach'", additionalExceptionData);
            }
        }

        private static JArray GenerateArrayWithForEachCommand(JObject redactSettings, JArray json)
        {
            JArray result = new JArray();
            
            foreach (JToken subJson in json)
            {
                result.Add(RecursiveEvaluateCommandsInRedactSettings(redactSettings, subJson));
            }

            return result;
        }

        private static bool RedactIfObjectContains(JToken redactSettings, JObject json)
        {
            return redactSettings.Cast<string>().All(key => json.ContainsKey(key) && JToken.DeepEquals(json[key], redactSettings[key]));
        }
        
        private static bool RedactOr(JArray orCommands, JToken json)
        {
            return orCommands.Select((t, i) => RecursiveEvaluateCommandsInRedactSettings((JObject) t, json[i])).Any(eval => (bool) eval);
        }
        
        private static bool RedactAnd(JArray andCommands, JToken json)
        {
            return andCommands.Select((t, i) => RecursiveEvaluateCommandsInRedactSettings((JObject) t, json[i])).All(eval => (bool) eval);
        }
    }
}
