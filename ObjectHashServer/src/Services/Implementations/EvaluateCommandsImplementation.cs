using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using ObjectHashServer.Exceptions;

namespace ObjectHashServer.Services.Implementations
{
    public static class EvaluateCommandsImplementation
    {
        /// <summary>
        /// If the redact settings contain command objects they will be evaluated here.
        /// Result is a redact setting containing only booleans.
        /// This method does change the provided inputs. So there is no need to clone
        /// them before calling this method.
        /// </summary>
        /// <returns>The fully evaluated redact settings object so that all JSON leaves
        /// only contain a boolean</returns>
        public static JToken EvaluateCommands(JToken redactSettings, JToken json)
        {
            return RecursiveEvaluateCommands(redactSettings.DeepClone(), json.DeepClone());
        }

        private static JToken RecursiveEvaluateCommands(JToken redactSettings, JToken json)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (redactSettings.Type)
            {
                case JTokenType.Array:
                    return RecursiveEvaluateJArray((JArray) redactSettings, json);
                case JTokenType.Object:
                    List<string> objectKeys = ((JObject) redactSettings).Properties().Select(p => p.Name).ToList();
                    if (objectKeys.Count == 1 && objectKeys[0].StartsWith("REDACT", Globals.STRING_COMPARE_METHOD))
                    {
                        return EvaluateSingleCommand(objectKeys[0], (JObject) redactSettings, json);
                    }
                    else
                    {
                        return RecursiveEvaluateJObject((JObject) redactSettings, json);
                    }
                case JTokenType.Boolean:
                    return redactSettings;
                default:
                    throw new BadRequestException(
                        "The redact settings are invalid. Commands can only be of type JSON object.");
            }
        }

        private static JToken RecursiveEvaluateJArray(JArray redactSettings, JToken json)
        {
            JArray result = new JArray();
            for (int i = 0; i < redactSettings.Count; i++)
            {
                result.Add(RecursiveEvaluateCommands(redactSettings[i], json[i]));
            }

            return result;
        }

        private static JToken RecursiveEvaluateJObject(JObject redactSettings, JToken json)
        {
            JObject result = new JObject();
            foreach ((string key, JToken jToken) in redactSettings)
            {
                result[key] = RecursiveEvaluateCommands(jToken, json[key]);
            }

            return result;
        }

        private static JToken EvaluateSingleCommand(string command, JObject redactSettings, JToken json)
        {
            switch (command)
            {
                case "REDACT:forEach":
                    try
                    {
                        return GenerateArrayWithForEachCommand(redactSettings["REDACT:forEach"], (JArray) json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply an forEach command. But the corresponding JSON was not an array.");
                    }
                case "REDACT:ifObjectContains":
                    try
                    {
                        return RedactIfObjectContains((JObject) redactSettings["REDACT:ifObjectContains"], (JObject) json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply the ifObjectContains function. But the corresponding JSON is not an object.");
                    }
                case "REDACT:or":
                    try
                    {
                        return RedactOr((JArray) redactSettings["REDACT:or"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply the or-Redact function. Please add the or commands as an array.");
                    }
                case "REDACT:and":
                    try
                    {
                        return RedactAnd((JArray) redactSettings["REDACT:and"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply the and-Redact function. Please add the and commands as an array.");
                    }
                default:
                    IDictionary additionalExceptionData = new Dictionary<string, object>
                    {
                        {"command", command}
                    };

                    throw new BadRequestException(
                        "You have tried to use a redact command. The command you used is not valid. Currently available: 'REDACT:forEach'",
                        additionalExceptionData);
            }
        }

        private static JArray GenerateArrayWithForEachCommand(JToken redactSettings, JArray json)
        {
            JArray result = new JArray();
            foreach (JToken subJson in json)
            {
                result.Add(RecursiveEvaluateCommands(redactSettings, subJson));
            }

            return result;
        }

        private static bool RedactIfObjectContains(JObject redactSettings, JObject json)
        {
            return redactSettings.Properties().Select(p => p.Name).All(key =>
                json.ContainsKey(key) && JToken.DeepEquals(json[key], redactSettings[key]));
        }

        private static bool RedactOr(JArray orCommands, JToken json)
        {
            return orCommands.Select((command, index) => RecursiveEvaluateCommands(command, json))
                .Any(eval => (bool) eval);
        }

        private static bool RedactAnd(JArray andCommands, JToken json)
        {
            return andCommands.Select((command, index) => RecursiveEvaluateCommands(command, json))
                .All(eval => (bool) eval);
        }
    }
}