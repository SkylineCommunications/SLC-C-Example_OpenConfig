namespace QAction_399.Utilities
{
	using Skyline.DataMiner.DataSources.CommunicationGatewayMiddleware.Common.Api;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// Implements the logging module for the OpenConfig Library.
	/// </summary>
	internal class MiddleWareLogger : ILogger
	{
		private readonly SLProtocol _protocol;

		/// <summary>
		/// Initializes a new instance of the <see cref="MiddleWareLogger"/> class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		public MiddleWareLogger(SLProtocol protocol)
		{
			_protocol = protocol;
		}

		/// <summary>
		/// Logs a message when the process encountered a problem that could not be handled without loss of functionality.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogFatal(string message)
		{
			_protocol.Log(message, LogType.Error, LogLevel.NoLogging);
		}

		/// <summary>
		/// Logs a message when the process encountered an exception when handling a request.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogError(string message)
		{
			_protocol.Log(message, LogType.Error, LogLevel.NoLogging);
		}

		/// <summary>
		/// Logs a message when the process received an invalid request.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogWarning(string message)
		{
			_protocol.Log(message, LogType.Error, LogLevel.NoLogging);
		}

		/// <summary>
		/// Logs a message for fundamental process execution.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogInfo(string message)
		{
			_protocol.Log(message, LogType.Information, LogLevel.NoLogging);
		}

		/// <summary>
		/// Logs a message when an action is undertaken.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogDebug(string message)
		{
			_protocol.Log(message, LogType.DebugInfo, LogLevel.Level2);
		}

		/// <summary>
		/// Logs a message to trace every action or function call being executed.
		/// </summary>
		/// <param name="message">Message to be logged.</param>
		public void LogTrace(string message)
		{
			_protocol.Log(message, LogType.DebugInfo, LogLevel.Level4);
		}
	}
}
