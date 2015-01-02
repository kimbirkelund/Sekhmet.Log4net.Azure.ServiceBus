using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using log4net.Appender;
using log4net.Core;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace Sekhmet.Log4net.Azure.ServiceBus
{
    public class ServiceBusAppender : AppenderSkeleton
    {
        private readonly SerialDisposable _loggingEventSubscription = new SerialDisposable();
        private readonly ManualResetEvent _processingCompletedWaitHandle = new ManualResetEvent(false);

        private string _connectionString;
        private IObserver<AzureLoggingEvent> _loggingEventObserver;
        private MessagingFactoryManager _messagingFactoryManager = new MessagingFactoryManager();

        public string ApplicationName { get; set; }
        public string ConnectionStringKey { get; set; }
        public string CorrelationIdPropertyName { get; set; }
        public string EventType { get; set; }
        public string MessagingEntity { get; set; }
        public bool Synchronous { get; set; }

        public override void ActivateOptions()
        {
            base.ActivateOptions();

            var connectionString = ConfigurationManager.ConnectionStrings[ConnectionStringKey];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
                throw new Exception("Cannot initialize appnder. Configured connection string name could not be found.");

            _connectionString = connectionString.ConnectionString;


            var subject = new Subject<AzureLoggingEvent>();
            _loggingEventObserver = subject;

            IObservable<IEnumerable<AzureLoggingEvent>> observable;
            if (Synchronous)
                observable = subject.Select(m => new[] { m });
            else
            {
                observable = subject.Buffer(TimeSpan.FromSeconds(5), 100)
                                    .Where(m => m.Any())
                                    .ObserveOn(new EventLoopScheduler());
            }

            _loggingEventSubscription.Disposable = observable.Subscribe(e => AppendInternal(e, 0),
                                                                        () => _processingCompletedWaitHandle.Set());
        }

        protected override void Append(LoggingEvent loggingEvent)
        {
            var azureLoggingEvent = new AzureLoggingEvent(loggingEvent)
                                    {
                                        ApplicationName = ApplicationName,
                                        EventType = EventType,
                                        CorrelationId = loggingEvent.LookupProperty(CorrelationIdPropertyName) as string
                                    };

            _loggingEventObserver.OnNext(azureLoggingEvent);
        }

        protected override void Append(LoggingEvent[] loggingEvents)
        {
            foreach (var loggingEvent in loggingEvents)
                Append(loggingEvent);
        }

        protected virtual void AppendInternal(IEnumerable<AzureLoggingEvent> loggingEvents, int attemptNo)
        {
            try
            {
                var messages = new List<BrokeredMessage>();

                foreach (var loggingEvent in loggingEvents)
                {
                    // Convert event to JSON
                    var stream = ConvertToJson(loggingEvent);

                    //Setup service bus message
                    var message = new BrokeredMessage(stream, true)
                                  {
                                      ContentType = "application/json",
                                      Label = loggingEvent.MessageType
                                  };
                    message.Properties.Add(new KeyValuePair<string, object>("ApplicationName", loggingEvent.ApplicationName));
                    message.Properties.Add(new KeyValuePair<string, object>("UserName", loggingEvent.UserName));
                    message.Properties.Add(new KeyValuePair<string, object>("MachineName", loggingEvent.MachineName));
                    message.Properties.Add(new KeyValuePair<string, object>("MessageType", loggingEvent.MessageType));
                    message.Properties.Add(new KeyValuePair<string, object>("Level", loggingEvent.Level));
                    message.Properties.Add(new KeyValuePair<string, object>("EventType", loggingEvent.EventType));

                    messages.Add(message);
                }

                //Setup Service Bus Connection
                var factory = _messagingFactoryManager.GetMessagingFactory(_connectionString);
                var sender = factory.CreateMessageSender(MessagingEntity);

                // Publish 
                sender.SendBatch(messages);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("The operation cannot be performed because the entity has been closed or aborted"))
                {
                    if (attemptNo < 3)
                        AppendInternal(loggingEvents, attemptNo + 1);
                    else
                        ErrorHandler.Error("Error occured while publishing error", ex);
                }
                else
                    ErrorHandler.Error("Error occured while publishing error", ex);
            }
        }

        protected virtual Stream ConvertToJson(AzureLoggingEvent loggingEvent)
        {
            var stream = new MemoryStream();

            var json = JsonConvert.SerializeObject(loggingEvent);

            var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();

            stream.Position = 0;
            return stream;
        }

        protected override void OnClose()
        {
            base.OnClose();

            _loggingEventObserver.OnCompleted();
            _processingCompletedWaitHandle.WaitOne();

            _loggingEventSubscription.Dispose();

            _messagingFactoryManager.Dispose();
            _messagingFactoryManager = null;
        }
    }
}