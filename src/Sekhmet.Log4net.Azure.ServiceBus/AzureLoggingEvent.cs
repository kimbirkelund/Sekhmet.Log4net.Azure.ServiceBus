using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using log4net.Core;

namespace Sekhmet.Log4net.Azure.ServiceBus
{
    [DataContract(Namespace = MessageNamespace, Name = MessageRootElement)]
    public class AzureLoggingEvent
    {
        public const string MessageNamespace = "urn://Sekhmet.Log4net.Azure.ServiceBus";
        public const string MessageRootElement = "AzureLoggingEvent";

        [DataMember]
        public string ApplicationName { get; set; }

        [DataMember]
        public string ClassName { get; set; }

        [DataMember]
        public string CorrelationId { get; set; }

        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public string EventType { get; set; }

        [DataMember]
        public string ExceptionMessage { get; set; }

        [DataMember]
        public string ExceptionString { get; set; }

        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string FullInfo { get; set; }

        [DataMember]
        public string Identity { get; set; }

        [DataMember]
        public string Level { get; set; }

        [DataMember]
        public string LineNumber { get; set; }

        [DataMember]
        public string LoggerName { get; set; }

        [DataMember]
        public string MachineName { get; set; }

        public string MessageType
        {
            get { return string.Format("{0}#{1}", MessageNamespace, MessageRootElement); }
        }

        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public Dictionary<string, string> Properties { get; set; }

        [DataMember]
        public string RenderedMessage { get; set; }

        [DataMember]
        public string ThreadName { get; set; }

        [DataMember]
        public DateTimeOffset Timestamp { get; set; }

        [DataMember]
        public DateTimeOffset TimestampAsUtc { get; set; }

        [DataMember]
        public string UserName { get; set; }

        public AzureLoggingEvent()
        {
            Properties = new Dictionary<string, string>();
        }

        public AzureLoggingEvent(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
                throw new ArgumentNullException("loggingEvent");

            Properties = new Dictionary<string, string>();

            MachineName = Environment.MachineName;
            Domain = loggingEvent.Domain;
            Identity = loggingEvent.Identity;
            Level = loggingEvent.Level.DisplayName;
            LoggerName = loggingEvent.LoggerName;
            RenderedMessage = loggingEvent.RenderedMessage;
            ThreadName = loggingEvent.ThreadName;
            Timestamp = loggingEvent.TimeStamp;
            UserName = loggingEvent.UserName;
            TimestampAsUtc = Timestamp.ToUniversalTime();

            if (loggingEvent.LocationInformation != null)
            {
                ClassName = loggingEvent.LocationInformation.ClassName;
                FileName = loggingEvent.LocationInformation.FileName;
                FullInfo = loggingEvent.LocationInformation.FullInfo;
                LineNumber = loggingEvent.LocationInformation.LineNumber;
                MethodName = loggingEvent.LocationInformation.MethodName;
            }

            var properties = loggingEvent.GetProperties();
            if (properties != null)
            {
                var keys = properties.GetKeys();
                foreach (var key in keys)
                {
                    var propertyValue = loggingEvent.LookupProperty(key);
                    Properties.Add(key, propertyValue.ToString());
                }
            }

            ExceptionString = loggingEvent.GetExceptionString();
            if (loggingEvent.ExceptionObject != null)
                ExceptionMessage = loggingEvent.ExceptionObject.Message;
        }
    }
}