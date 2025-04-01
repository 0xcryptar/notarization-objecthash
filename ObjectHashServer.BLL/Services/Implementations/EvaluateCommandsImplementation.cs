using Newtonsoft.Json.Linq;
using ObjectHashServer.BLL.Exceptions;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

[assembly: InternalsVisibleToAttribute("ObjectHashServer.UnitTests")]

namespace ObjectHashServer.BLL.Services.Implementations
{
    public static class EvaluateCommandsImplementation
    {
        /// <summary>
        /// If the redact settings contain command objects they will be evaluated here.
        /// Result is a redact setting containing only booleans.
        /// This method does change the provided inputs. So there is no need to clone
        /// them before calling this method.
        /// Members that are missing from the redaction setting will be redacted according the given default redaction behaviour.
        /// </summary>
        /// <returns>The fully evaluated redact settings object so that all JSON leaves
        /// only contain a boolean</returns>
        public static JToken EvaluateCommands(JToken redactSettings, JToken json, bool defaultLeaveRedaction = true)
        {
            var recursiveEvaluatedRedactionSettings = RecursiveEvaluateCommands(redactSettings.DeepClone(), json.DeepClone());

            RecursiveExtendStructureWithDefault(recursiveEvaluatedRedactionSettings, json, defaultLeaveRedaction);

            return recursiveEvaluatedRedactionSettings;
        }

        /// <summary>
        /// Extends the structure an evaluated redaction settings to match exactly that of the json with given default values.
        /// </summary>
        /// <param name="recursiveEvaluatedRedactionSettings"></param>
        /// <param name="json"></param>
        /// <param name="defaultLeaveRedaction"></param>
        internal static void RecursiveExtendStructureWithDefault(JToken redactSettings, JToken json, bool defaultLeaveRedaction)
        {
            switch (json.Type)
            {
                case JTokenType.Object:
                    HandleObjectType(redactSettings, json, defaultLeaveRedaction);
                    break;

                case JTokenType.Array:
                    HandleArrayType(redactSettings, json, defaultLeaveRedaction);
                    break;
                default:
                    throw new ArgumentException("This token type should not be reachable.");
            }
        }

        private static void HandleObjectType(JToken redactSettings, JToken json, bool defaultLeaveRedaction)
        {
            var jsonObject = (JObject)json;
            var redactObject = (JObject)redactSettings;
            foreach (var jprop in jsonObject.Properties())
            {
                if (redactObject.ContainsKey(jprop.Name))
                {
                    HandleExistingProperty(redactObject, jprop, defaultLeaveRedaction);
                }
                else
                {
                    HandleMissingProperty(redactObject, jprop, defaultLeaveRedaction);
                }
            }
        }

        private static void HandleExistingProperty(JObject redactObject, JProperty jprop, bool defaultLeaveRedaction)
        {
            if (redactObject[jprop.Name].Type == JTokenType.Boolean)
            {
                // this is good, already set, simply ignore
            }
            else if (redactObject[jprop.Name].Type == JTokenType.Array || redactObject[jprop.Name].Type == JTokenType.Object)
            {
                // fill recursively
                RecursiveExtendStructureWithDefault(redactObject[jprop.Name], jprop.Value, defaultLeaveRedaction);
            }
            else
            {
                // existing redaction setting that is neither array nor object and not a bool value should not be the case
                throw new ArgumentException("We expect bool values or arrays or objects here.");
            }
        }

        private static void HandleMissingProperty(JObject redactObject, JProperty jprop, bool defaultLeaveRedaction)
        {
            switch (jprop.Value.Type)
            {
                // leaf values should simply produce a default redaction behaviour
                case JTokenType.None:
                case JTokenType.Integer:
                case JTokenType.Float:
                case JTokenType.String:
                case JTokenType.Boolean:
                case JTokenType.Null:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Bytes:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                    redactObject[jprop.Name] = defaultLeaveRedaction;
                    break;

                // missing objects should be created and recursively populated
                case JTokenType.Object:
                    redactObject[jprop.Name] = new JObject();
                    RecursiveExtendStructureWithDefault(redactObject[jprop.Name], jprop.Value, defaultLeaveRedaction);
                    break;

                case JTokenType.Array:
                    redactObject[jprop.Name] = new JArray();
                    RecursiveExtendStructureWithDefault(redactObject[jprop.Name], jprop.Value, defaultLeaveRedaction);
                    break;
                default:
                    throw new ArgumentException("This token type should not be reachable.");
            }
        }

        private static void HandleArrayType(JToken redactSettings, JToken json, bool defaultLeaveRedaction)
        {
            var jsonArray = (JArray)json;
            var redactArray = (JArray)redactSettings;

            // add missing entries with proper type to add default values
            if (redactArray.Count < jsonArray.Count)
            {
                AddMissingEntries(redactArray, jsonArray, defaultLeaveRedaction);
            }

            for (int i = 0; i < redactArray.Count; ++i)
            {
                if (redactArray[i].Type == JTokenType.Array || redactArray[i].Type == JTokenType.Object)
                    RecursiveExtendStructureWithDefault(redactArray[i], jsonArray[i], defaultLeaveRedaction);
            }
        }

        private static void AddMissingEntries(JArray redactArray, JArray jsonArray, bool defaultLeaveRedaction)
        {
            // add nodes of proper type to the array
            // value nodes should get the redaction bool setting arrays and objects should be emtpy
            for (int vi = redactArray.Count; vi < jsonArray.Count; ++vi)
            {
                var vijson = jsonArray[vi];
                switch (vijson.Type)
                {
                    // leaf values should simply produce a default redaction behaviour
                    case JTokenType.None:
                    case JTokenType.Integer:
                    case JTokenType.Float:
                    case JTokenType.String:
                    case JTokenType.Boolean:
                    case JTokenType.Null:
                    case JTokenType.Undefined:
                    case JTokenType.Date:
                    case JTokenType.Raw:
                    case JTokenType.Bytes:
                    case JTokenType.Guid:
                    case JTokenType.Uri:
                    case JTokenType.TimeSpan:
                        redactArray.Add(defaultLeaveRedaction);
                        break;

                    // missing objects should be created
                    case JTokenType.Object:
                        redactArray.Add(new JObject());
                        break;

                    case JTokenType.Array:
                        redactArray.Add(new JArray());
                        break;
                    default:
                        throw new ArgumentException("This token type should not be reachable.");
                }
            } 
        }

        private static JToken RecursiveEvaluateCommands(JToken redactSettings, JToken json)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (redactSettings.Type)
            {
                case JTokenType.Array:
                    return RecursiveEvaluateJArray((JArray)redactSettings, json);
                case JTokenType.Object:
                    List<string> objectKeys = ((JObject)redactSettings).Properties().Select(p => p.Name).ToList();
                    if (objectKeys.Count == 1 && objectKeys[0].StartsWith("REDACT", Globals.STRING_COMPARE_METHOD))
                    {
                        return EvaluateSingleCommand(objectKeys[0], (JObject)redactSettings, json);
                    }
                    else
                    {
                        return RecursiveEvaluateJObject((JObject)redactSettings, json);
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
            for (int i = 0; i < Math.Min(redactSettings.Count, json.Count()); i++)
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
                        return GenerateArrayWithForEachCommand(redactSettings["REDACT:forEach"], (JArray)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply an forEach command. But the corresponding JSON was not an array.");
                    }
                case "REDACT:ifObjectContains":
                    try
                    {
                        return RedactIfObjectContains((JObject)redactSettings["REDACT:ifObjectContains"], (JObject)json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply the ifObjectContains function. But the corresponding JSON is not an object.");
                    }
                case "REDACT:or":
                    try
                    {
                        return RedactOr((JArray)redactSettings["REDACT:or"], json);
                    }
                    catch (InvalidCastException)
                    {
                        throw new BadRequestException(
                            "You have tried to apply the or-Redact function. Please add the or commands as an array.");
                    }
                case "REDACT:and":
                    try
                    {
                        return RedactAnd((JArray)redactSettings["REDACT:and"], json);
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
                .Any(eval => (bool)eval);
        }

        private static bool RedactAnd(JArray andCommands, JToken json)
        {
            return andCommands.Select((command, index) => RecursiveEvaluateCommands(command, json))
                .All(eval => (bool)eval);
        }
    }
}