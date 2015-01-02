using System;
using log4net;
using log4net.Config;

namespace Sekhmet.Log4net.Azure.ServiceBus.TestApplication
{
    internal class Program
    {
        private static void Main()
        {
            XmlConfigurator.Configure();
            var applicationLogger = LogManager.GetLogger(typeof(Program));
            var auditLogger = LogManager.GetLogger("AuditLogger");
            var globalLogger = LogManager.GetLogger("GlobalLogger");

            ThreadContext.Properties["CorrelationId"] = Guid.NewGuid();

            applicationLogger.Debug("Program starting");

            for (int i = 0; i <= 1000; i++)
            {
                applicationLogger.Info("This is info");
                globalLogger.Info("The application has started");
            }

            //lots of code.....
            applicationLogger.Debug("Ive done something");

            try
            {
                applicationLogger.Debug("About to audit");
                auditLogger.Info("About to do something important");
                applicationLogger.Debug("Audit complete");
                //.... doing some work
                throw new ApplicationException("This is an error");
            }
            catch (Exception ex)
            {
                //... log error locally
                applicationLogger.Error("There was an error", ex);
                //... send error to central error system
                globalLogger.Error("There was an error", ex);
            }

            LogManager.Shutdown();
            Console.WriteLine("LogManager shut down");
            Console.ReadLine();
        }
    }
}