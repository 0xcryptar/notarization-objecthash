using Newtonsoft.Json.Linq;
using NUnit.Framework;
using ObjectHashServer.BLL.Services.Implementations;

namespace ObjectHashServer.UnitTests.Services.Implementations
{
    [TestFixture]
    public class EvaluateCommandsImplementationTests
    {
        // cases
        // array
        // object pure
        // object commands
        // boolean

        [Test]
        public void RecursiveExtendStructureWithDefault_EmptyArrayInRedactionSettings_DefaultsAdded()
        {
            JToken json = JToken.Parse(@"{
	""array"":[
	  ""val 1"",
	  ""val 2"",
	  ""val 3"",
	  ""val 4"",
	]
}");

            JToken redactionSettings = JToken.Parse(@"{
	""array"":[
	]
}");

            bool defaultValueForRedaction = true;
            EvaluateCommandsImplementation.RecursiveExtendStructureWithDefault(redactionSettings, json, defaultValueForRedaction);


            var array = (JArray)redactionSettings["array"];
            Assert.AreEqual(4, array.Count);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[0]);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[1]);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[2]);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[3]);
        }

        [Test]
        public void RecursiveExtendStructureWithDefault_FewerEntriesInRedactionSettings_DefaultsAdded()
        {
            JToken json = JToken.Parse(@"{
	""array"":[
	  ""val 1"",
	  ""val 2"",
	  ""val 3"",
	  ""val 4"",
	]
}");

            JToken redactionSettings = JToken.Parse(@"{
	""array"":[
	  false,
	]
}");

            bool defaultValueForRedaction = true;
            EvaluateCommandsImplementation.RecursiveExtendStructureWithDefault(redactionSettings, json, defaultValueForRedaction);


            var array = (JArray)redactionSettings["array"];
            Assert.AreEqual(4, array.Count);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[1]);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[2]);
            Assert.AreEqual(defaultValueForRedaction, (bool)array[3]);
        }

        [Test]
        public void RecursiveExtendStructureWithDefault_MoreEntriesInRedactionSettings_NoChanges()
        {
            JToken json = JToken.Parse(@"{
	""array"":[
	  ""val 1"",
	  ""val 2"",
	  ""val 3"",
	  ""val 4"",
	]
}");

            JToken redactionSettings = JToken.Parse(@"{
	""array"":[
	  false,
      false,
      false,
      false,
      false,
      false,
      false
	]
}");

            var originalRedactSettings = redactionSettings.DeepClone();

            bool defaultValueForRedaction = true;
            EvaluateCommandsImplementation.RecursiveExtendStructureWithDefault(redactionSettings, json, defaultValueForRedaction);


            Assert.AreEqual(originalRedactSettings, redactionSettings);
        }
    }
}