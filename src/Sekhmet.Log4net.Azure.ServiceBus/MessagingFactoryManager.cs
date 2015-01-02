using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Microsoft.ServiceBus.Messaging;

namespace Sekhmet.Log4net.Azure.ServiceBus
{
    /// <summary>
    ///     Manages access to the message factories so that the performance can be managed to ensure
    ///     the manager is refreshed to stop slow first time access
    /// </summary>
    internal class MessagingFactoryManager : IDisposable
    {
        private static readonly TimeSpan _messagingFactoryRefreshInterval = TimeSpan.FromMinutes(1);

        private readonly object _mutex = new object();
        private Timer _messageFactoryTimer;
        private Dictionary<string, MessagingFactoryWrapper> _messagingFactoryWrappers = new Dictionary<string, MessagingFactoryWrapper>();

        public bool IsDiposed { get; private set; }

        public MessagingFactoryManager()
        {
            _messageFactoryTimer = new Timer(_messagingFactoryRefreshInterval.TotalMilliseconds);
            _messageFactoryTimer.Elapsed += _messageFactoryTimer_Elapsed;
        }

        public void Dispose()
        {
            lock (_mutex)
            {
                if (IsDiposed)
                    return;
                IsDiposed = true;
            }

            var messageFactoryTimer = _messageFactoryTimer;
            if (messageFactoryTimer != null)
                messageFactoryTimer.Dispose();
            _messageFactoryTimer = null;

            var messagingFactoryWrappers = _messagingFactoryWrappers;
            if (messagingFactoryWrappers != null)
            {
                var messagingFactoryWrappersList = messagingFactoryWrappers.Values.ToList();
                foreach (var messagingFactoryWrapper in messagingFactoryWrappersList)
                    messagingFactoryWrapper.Dispose();
            }
            _messagingFactoryWrappers = null;
        }

        /// <summary>
        ///     Gets the message factory from the factory manager
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public MessagingFactory GetMessagingFactory(string connectionString)
        {
            lock (_mutex)
                if (IsDiposed)
                    return null;

            var wrapper = GetMessageFactoryWrapper(connectionString);
            if (wrapper == null || wrapper.Age > _messagingFactoryRefreshInterval.Add(TimeSpan.FromSeconds(5)))
            {
                RefreshMessagingFactory(connectionString);
                wrapper = GetMessageFactoryWrapper(connectionString);
            }
            return wrapper.Factory;
        }

        /// <summary>
        ///     Get the message factory using a semaphore to create a pool around the resources
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private MessagingFactoryWrapper GetMessageFactoryWrapper(string connectionString)
        {
            lock (_mutex)
            {
                if (IsDiposed)
                    return null;

                MessagingFactoryWrapper messagingFactoryWrapper;
                _messagingFactoryWrappers.TryGetValue(connectionString, out messagingFactoryWrapper);
                return messagingFactoryWrapper;
            }
        }

        /// <summary>
        ///     Refreshes messaging factories which may be close to expiring
        /// </summary>
        /// <param name="connectionString"></param>
        /// <remarks>
        ///     Attribute ensures method only executed one thread at a time
        /// </remarks>
        private void RefreshMessagingFactory(string connectionString)
        {
            var wrapper = GetMessageFactoryWrapper(connectionString);

            if (wrapper == null)
            {
                // Create first time
                var factory = MessagingFactory.CreateFromConnectionString(connectionString);
                wrapper = new MessagingFactoryWrapper(factory);
                ReplaceMessageFactory(connectionString, wrapper);
            }
            else
            {
                if (wrapper.Age < _messagingFactoryRefreshInterval)
                    return; //Doesnt need refreshing

                //Refresh the factory                
                var factory = MessagingFactory.CreateFromConnectionString(connectionString);
                wrapper = new MessagingFactoryWrapper(factory);
                ReplaceMessageFactory(connectionString, wrapper);
            }
        }

        /// <summary>
        ///     Replaces the factory using a semaphore to create a pool around the resources
        /// </summary>
        private void ReplaceMessageFactory(string connectionString, MessagingFactoryWrapper wrapper)
        {
            lock (_mutex)
            {
                if (IsDiposed)
                    return;

                MessagingFactoryWrapper currentMessagingFactoryWrapper;
                if (_messagingFactoryWrappers.TryGetValue(connectionString, out currentMessagingFactoryWrapper))
                    currentMessagingFactoryWrapper.Dispose();

                _messagingFactoryWrappers[connectionString] = wrapper;
            }
        }

        /// <summary>
        ///     Timer trigger to refresh message factories
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void _messageFactoryTimer_Elapsed(object sender, ElapsedEventArgs args)
        {
            List<KeyValuePair<string, MessagingFactoryWrapper>> factoryWrappers;
            lock (_mutex)
            {
                if (IsDiposed)
                    return;

                factoryWrappers = _messagingFactoryWrappers.Where(factoryWrapper => factoryWrapper.Value.Age > _messagingFactoryRefreshInterval)
                                                           .ToList();
            }

            foreach (var factoryWrapper in factoryWrappers)
                RefreshMessagingFactory(factoryWrapper.Key);
        }
    }
}