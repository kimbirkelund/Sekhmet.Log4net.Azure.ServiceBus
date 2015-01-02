using System;
using Microsoft.ServiceBus.Messaging;

namespace Sekhmet.Log4net.Azure.ServiceBus
{
    internal class MessagingFactoryWrapper : IDisposable
    {
        public TimeSpan Age
        {
            get { return DateTime.Now - CreatedAt; }
        }

        public DateTime CreatedAt { get; private set; }
        public MessagingFactory Factory { get; private set; }

        public MessagingFactoryWrapper(MessagingFactory factory)
        {
            Factory = factory;
            CreatedAt = DateTime.Now;
        }

        public void Dispose()
        {
            var factory = Factory;
            Factory = null;

            if (factory != null)
                factory.Close();
        }
    }
}