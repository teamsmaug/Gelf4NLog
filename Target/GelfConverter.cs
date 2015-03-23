using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Newtonsoft.Json.Linq;

namespace Gelf4NLog.Target
{
    public class GelfConverter : IConverter
    {
        private const int ShortMessageMaxLength = 250;
        private const string GelfVersion = "1.1";

        private static readonly CamelCasePropertyNamesContractResolver PropertyResolver = new CamelCasePropertyNamesContractResolver();

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            NullValueHandling = NullValueHandling.Include,
            ContractResolver = PropertyResolver
        };

        public JObject GetGelfJson(LogEventInfo logEventInfo, string facility)
        {
            //Retrieve the formatted message from LogEventInfo
            var logEventMessage = logEventInfo.FormattedMessage;
            if (logEventMessage == null) return null;

            //If we are dealing with an exception, pass exception properties to LogEventInfo properties
            if (logEventInfo.Exception != null)
            {
                logEventInfo.Properties.Add("ExceptionSource", logEventInfo.Exception.Source);
                logEventInfo.Properties.Add("ExceptionMessage", logEventInfo.Exception.Message);
                logEventInfo.Properties.Add("StackTrace", logEventInfo.Exception.StackTrace);
            }

            //Figure out the short message
            var shortMessage = logEventMessage;
            if (shortMessage.Length > ShortMessageMaxLength)
            {
                shortMessage = shortMessage.Substring(0, ShortMessageMaxLength);
            }

            //Construct the instance of GelfMessage
            //See https://github.com/Graylog2/graylog2-docs/wiki/GELF "Specification (version 1.0)"
            var gelfMessage = new GelfMessage
                                  {
                                      Version = GelfVersion,
                                      Host = Dns.GetHostName(),
                                      ShortMessage = shortMessage,
                                      FullMessage = logEventMessage,
                                      Timestamp = logEventInfo.TimeStamp,
                                      Level = GetSeverityLevel(logEventInfo.Level)
                                  };

            //Convert to JSON
            var jsonObject = JObject.FromObject(gelfMessage);

            //Add any other interesting data to LogEventInfo properties
            logEventInfo.Properties.Add("loggerName", logEventInfo.LoggerName);

            if (!string.IsNullOrWhiteSpace(facility))
            {
                logEventInfo.Properties.Add("facility", facility);
            }

            var line = GetLine(logEventInfo);
            if (!string.IsNullOrWhiteSpace(line))
            {
                logEventInfo.Properties.Add("line", line);
            }

            var file = GetFile(logEventInfo);
            if (!string.IsNullOrWhiteSpace(file))
            {
                logEventInfo.Properties.Add("file", file);
            }

            //We will persist them "Additional Fields" according to Gelf spec
            foreach (var property in logEventInfo.Properties)
            {
                AddAdditionalField(jsonObject, property);
            }

            return jsonObject;
        }

        private static string GetFile(LogEventInfo logEventInfo)
        {
            return (logEventInfo.UserStackFrame != null)
                ? logEventInfo.UserStackFrame.GetFileName()
                : string.Empty;
        }

        private static string GetLine(LogEventInfo logEventInfo)
        {
            return (logEventInfo.UserStackFrame != null)
                ? logEventInfo.UserStackFrame.GetFileLineNumber().ToString(
                    CultureInfo.InvariantCulture)
                : string.Empty;
        }

        private static void FlattenAndAddObject(IDictionary<string, JToken> gelfMessage, string key, IEnumerable<JToken> values)
        {
            foreach (var value in values)
            {
                if (value is JProperty)
                {
                    var property = value as JProperty;

                    var flattenedKey = string.Concat(key, "_", property.Name);
                    if (!property.Value.HasValues)
                    {
                        gelfMessage.Add(flattenedKey, property.Value);
                    }

                    FlattenAndAddObject(gelfMessage, flattenedKey, property.Children());
                }

                if (value is JObject)
                {
                    FlattenAndAddObject(gelfMessage, key, value);
                }
            }
        }

        private static void AddAdditionalField(IDictionary<string, JToken> jObject, KeyValuePair<object, object> property)
        {
            var key = property.Key as string;
            if (key == null) return;

            key = PropertyResolver.GetResolvedPropertyName(key);

            //According to the GELF spec, libraries should NOT allow to send id as additional field (_id)
            //Server MUST skip the field because it could override the MongoDB _key field
            if (key.Equals("id", StringComparison.OrdinalIgnoreCase))
                key = "id_";

            //According to the GELF spec, additional field keys should start with '_' to avoid collision
            if (!key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                key = "_" + key;

            if (property.Value == null)
            {
                jObject.Add(key, null);
                return;
            }
                
            var value = JToken.FromObject(property.Value, Serializer);
            FlattenAndAddObject(jObject, key, value);

            if (!value.HasValues)
            {
                jObject.Add(key, value);
            }
        }

        /// <summary>
        /// Values from SyslogSeverity enum here: http://marc.info/?l=log4net-dev&m=109519564630799
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private static int GetSeverityLevel(LogLevel level)
        {
            if (level == LogLevel.Debug)
            {
                return 7;
            }
            if (level == LogLevel.Fatal)
            {
                return 2;
            }
            if (level == LogLevel.Info)
            {
                return 6;
            }
            if (level == LogLevel.Trace)
            {
                return 6;
            }
            if (level == LogLevel.Warn)
            {
                return 4;
            }

            return 3; //LogLevel.Error
        }
    }
}
