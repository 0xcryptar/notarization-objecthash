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
        /// </summary>
        /// <returns>The fully evaluated redact settings object so that all JSON leaves only contain a boolean</returns>
        public static JToken EvaluateCommandsInRedactSettings(JToken redactSettings, JToken json)
        {
            return RecursiveEvaluateCommandsInRedactSettings(redactSettings.DeepClone(), json.DeepClone());
        }
        
        private static JToken RecursiveEvaluateCommandsInRedactSettings(JToken redactSettings, JToken json)
        {
            switch (redactSettings.Type)
            {
                case JTokenType.Array:
                    JArray result = new JArray();
                    for (int i = 0; i < ((JArray)redactSettings).Count; i++)
                    {
                        result.Add(RecursiveEvaluateCommandsInRedactSettings(redactSettings[i], json[i]));
                    }

                    return result;
                case JTokenType.Object:
                    List<string> objectKeys = ((JObject)redactSettings).Properties().Select(p => p.Name).ToList();
                    if (objectKeys.Count != 1 || !objectKeys[0].StartsWith("REDACT", Globals.STRING_COMPARE_METHOD))
                    {
                        return EvaluateCommandInJObject((JObject)redactSettings, json);
                    }
                    else
                    {
                        // TODO: ?for each? implement
                        return null;
                    }
                case JTokenType.Boolean:
                    return redactSettings;
                default:
                    throw new BadRequestException("wrong");
            }
        }

        private static JToken EvaluateCommandInJObject(JObject command, JToken json)
        {            
            List<string> objectKeys = command.Properties().Select(p => p.Name).ToList();
            switch (objectKeys[0])
            {
                case "REDACT:forEach":
                    try
                    {
                        return GenerateArrayWithForEachCommand((JObject)command["REDACT:forEach"], (JArray)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply an forEach command. But the corresponding JSON was not an array.");
                    }
                case "REDACT:ifObjectContains":
                    try
                    {
                        return RedactIfObjectContains(command["REDACT:ifObjectContains"], (JObject)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the ifObjectContains function. But the corresponding JSON is not an object.");
                    }
                case "REDACT:or":
                    try
                    {
                        return RedactOr((JArray)command["REDACT:or"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the or-Redact function. Please add the or commands as an array.");
                    }
                case "REDACT:and":
                    try
                    {
                        return RedactAnd((JArray)command["REDACT:and"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException("You have tried to apply the and-Redact function. Please add the and commands as an array.");
                    }
                default:
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        { "commandObject", command }
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
