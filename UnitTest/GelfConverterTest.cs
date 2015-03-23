using System;
using System.Net;
using Gelf4NLog.Target;
using NLog;
using NUnit.Framework;

namespace Gelf4NLog.UnitTest
{
    public class GelfConverterTest
    {
        [TestFixture(Category = "GelfConverter")]
        public class GetGelfJsonMethod
        {
            [Test]
            public void ShouldCreateGelfJsonCorrectlyWithFlattenedExtraObjects()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                {
                    Message = "Test Log Message",
                    Level = LogLevel.Info,
                    TimeStamp = timestamp,
                    LoggerName = "GelfConverterTestLogger"
                };
                logEvent.Properties.Add("customproperty4", new[] { 1, 2, 3 });
                logEvent.Properties.Add("_customproperty1", "customvalue1");
                logEvent.Properties.Add("_customproperty2", new { Value1 = "customvalue1", Value2 = "customvalue2", Extra2 = new { Value3 = "customvalue3" } });
                logEvent.Properties.Add("customproperty3", 2);
                

                var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("1.1", jsonObject.Value<string>("version"));
                Assert.AreEqual(Dns.GetHostName(), jsonObject.Value<string>("host"));
                Assert.AreEqual("Test Log Message", jsonObject.Value<string>("short_message"));
                Assert.AreEqual("Test Log Message", jsonObject.Value<string>("full_message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("timestamp"));
                Assert.AreEqual(6, jsonObject.Value<int>("level"));

                Assert.AreEqual("customvalue1", jsonObject.Value<string>("_customproperty1"));
                Assert.AreEqual("customvalue1", jsonObject.Value<string>("_customproperty2_value1"));
                Assert.AreEqual("customvalue2", jsonObject.Value<string>("_customproperty2_value2"));
                Assert.AreEqual("customvalue3", jsonObject.Value<string>("_customproperty2_extra2_value3"));
                Assert.AreEqual("GelfConverterTestLogger", jsonObject.Value<string>("_loggerName"));
                Assert.AreEqual("TestFacility", jsonObject.Value<string>("_facility"));
                Assert.AreEqual(2, jsonObject.Value<int>("_customproperty3"));

                //make sure that there are no other junk in there
                Assert.AreEqual(13, jsonObject.Count);
            }

            [Test]
            public void ShouldCreateGelfJsonCorrectly()
            {
                var timestamp = DateTime.Now;
                var logEvent = new LogEventInfo
                                   {
                                       Message = "Test Log Message", 
                                       Level = LogLevel.Info, 
                                       TimeStamp = timestamp,
                                       LoggerName = "GelfConverterTestLogger"
                                   };
                logEvent.Properties.Add("customproperty1", "customvalue1");
                logEvent.Properties.Add("customproperty2", "customvalue2");

                var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("1.1", jsonObject.Value<string>("version"));
                Assert.AreEqual(Dns.GetHostName(), jsonObject.Value<string>("host"));
                Assert.AreEqual("Test Log Message", jsonObject.Value<string>("short_message"));
                Assert.AreEqual("Test Log Message", jsonObject.Value<string>("full_message"));
                Assert.AreEqual(timestamp, jsonObject.Value<DateTime>("timestamp"));
                Assert.AreEqual(6, jsonObject.Value<int>("level"));
                Assert.AreEqual("TestFacility", jsonObject.Value<string>("_facility"));
                
                Assert.AreEqual("customvalue1", jsonObject.Value<string>("_customproperty1"));
                Assert.AreEqual("customvalue2", jsonObject.Value<string>("_customproperty2"));
                Assert.AreEqual("GelfConverterTestLogger", jsonObject.Value<string>("_loggerName"));
                
                //make sure that there are no other junk in there
                Assert.AreEqual(10, jsonObject.Count);
            }

            [Test]
            public void ShouldHandleExceptionsCorrectly()
            {
                var logEvent = new LogEventInfo
                                   {
                                       Message = "Test Message",
                                       Exception = new DivideByZeroException("div by 0")
                                   };

                var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual("Test Message", jsonObject.Value<string>("short_message"));
                Assert.AreEqual("Test Message", jsonObject.Value<string>("full_message"));
                Assert.AreEqual(3, jsonObject.Value<int>("level"));
                Assert.AreEqual("TestFacility", jsonObject.Value<string>("_facility"));
                Assert.AreEqual(null, jsonObject.Value<string>("_exceptionSource"));
                Assert.AreEqual("div by 0", jsonObject.Value<string>("_exceptionMessage"));
                Assert.AreEqual(null, jsonObject.Value<string>("_stackTrace"));
                Assert.AreEqual(null, jsonObject.Value<string>("_loggerName"));
            }

            [Test]
            public void ShouldHandleLongMessageCorrectly()
            {
                var logEvent = new LogEventInfo
                {
                    //The first 300 chars of lorem ipsum...
                    Message = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Phasellus interdum est in est cursus vitae pellentesque felis lobortis. Donec a orci quis ante viverra eleifend ac et quam. Donec imperdiet libero ut justo tincidunt non tristique mauris gravida. Fusce sapien eros, tincidunt a placerat nullam."
                };

                var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

                Assert.IsNotNull(jsonObject);
                Assert.AreEqual(250, jsonObject.Value<string>("short_message").Length);
                Assert.AreEqual(300, jsonObject.Value<string>("full_message").Length);
            }

            [Test]
            public void ShouldHandlePropertyCalledIdProperly()
            {
                var logEvent = new LogEventInfo { Message = "Test" };
                logEvent.Properties.Add("Id", "not_important");

                var jsonObject = new GelfConverter().GetGelfJson(logEvent, "TestFacility");

                Assert.IsNotNull(jsonObject);
                Assert.IsNull(jsonObject["_id"]);
                Assert.AreEqual("not_important", jsonObject.Value<string>("_id_"));
            }
        }
    }
}
