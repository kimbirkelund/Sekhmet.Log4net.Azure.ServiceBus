Sekhmet.Log4net.Azure.ServiceBus
================================

Log4net appender for writing to an Azure ServiceBus queue.

Code is taken from Michael Stephensons blog post on the subject (http://geekswithblogs.net/michaelstephenson/archive/2014/01/02/155044.aspx). Same goes for the configuration examples and description. 

# Appender Configuration

Below is an example of the appender configuration you would use for the ServiceBusAppender in your log4net configuration file.

```xml
<appender name="AzureServiceBus-Logging-Appender"
          type="Sekhmet.Log4net.Azure.ServiceBus.ServiceBusAppender, Sekhmet.Log4net.Azure.ServiceBus">
	<ConnectionStringKey value="Log4netServiceBusConnection" />
	<MessagingEntity value="log" />
	<ApplicationName value="Sekhmet.Log4net.Azure.ServiceBus.TestApplication" />
	<EventType value="LoggingEvent" />
	<CorrelationIdPropertyName value="CorrelationId" />
	<Synchronous value="false" />
	<filter type="log4net.Filter.LevelRangeFilter">
		<levelMin value="INFO" />
		<levelMax value="FATAL" />
	</filter>
</appender>
```

There are a couple of key properties here:

 - **ConnectionStringKey**: This property is a string which points to a connection string you have declared in the config file. The connection string you have declared will be a connection string for a Windows Azure Service Bus Namespace

 - **MessagingEntity**: This is the name of a queue or topic in the Windows Azure Service bus which you would like to publish the message to

 - **ApplicationName**: The application name property will be used to indicate on the log message which application published the message.

 - **EventType**: The event type is used to indicate what type of event it is. An example of where you might use this is if you have multiple loggers using different instances of the appender which are configured differently, then you might use this to indicate the types of events being published. An example might be publishing audit events and standard logging events.

 - **CorrelationIdPropertyName**: Normally as you process flows across components you would include in this flow some context to relate execution in different places. An example of how you might do this would be to create a unique id when the user first clicks a button and then flow this through a web service call and into another component. From here you would usually use the log4net ThreadContext to set these variables to be available to each component as they begin executing. In this case the correlation id is a useful property to allow us to relate log messages from different components. In the configuration the CorrelationIdPropertyName allows you to specify the name of the log4net property that holds the CorrelationId in this component. This correlation id is then used as a specific element on the published log event.

 - **Synchronous**: The synchronous property allows you to control if an event is published on a background thread or not. There is a performance gain in publishing on a background thread but the trade of is you can't as easily respond to errors. For normal logging events you're probably happy to swallow the error and continue, but perhaps for audit events you might not want to do this.

In addition to the specific configuration properties the rest of the normal log4net stuff pretty much applies. As an example you can use the Appender filtering mechanism to control which messages are processed by the appender.