namespace QAction_399.ModelHelpers
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json;
	using QAction_399.Models;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Api;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Models;
	using Skyline.DataMiner.Scripting;

	internal static class SystemHelper
	{
		/// <summary>
		/// Polls the system state group with all paths known by the DataMapper. Parameters will be filled in automatically, no further parsing will be needed.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="client">Link with the gNMI client.</param>
		/// <param name="systemGroup">Group with paths to be polled.</param>
		public static void PollSystemState(SLProtocol protocol, GnmiClient client, List<Gnmi.Path> systemGroup)
		{
			try
			{
				client.Get(systemGroup);
			}
			catch (Exception ex)
			{
				protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|PollSystemState|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
			}
		}

		/// <summary>
		/// Polls the OpenFlow path that is known by the DataMapper. Parameter table will be filled in automatically, no further parsing will be needed.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="client">Link with the gNMI client.</param>
		/// <param name="openflowGroup">Group with path to be polled.</param>
		public static void PollOpenFlowDataMapper(SLProtocol protocol, GnmiClient client, List<Gnmi.Path> openflowGroup)
		{
			try
			{
				client.Get(openflowGroup); // Note: according to specifications this is intended to retrieve relatively small sets of data. Use Subscribe RPC for retrieving very large data sets. Timestamp is also not advised to be used here.
			}
			catch (Exception ex)
			{
				protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|PollOpenFlowDataMapper|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
			}
		}

		/// <summary>
		/// Polls the OpenFlow path that is not known by the DataMapper. The parsing of the responses will be done manually.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="client">Link with the gNMI client.</param>
		public static void PollOpenFlowManualParsing(SLProtocol protocol, GnmiClient client)
		{
			try
			{
				// Note: according to specifications this is intended to retrieve relatively small sets of data. Use Subscribe RPC for retrieving very large data sets. Timestamp is also not advised to be used here.
				foreach (var value in client.Get(new List<string> { "system/openflow/controllers/controller[name='second']/connections" }))
				{
					FillSecondConnections(protocol, value);
				}
			}
			catch (Exception ex)
			{
				protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|PollOpenFlowManualParsing|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
			}
		}

		/// <summary>
		/// Sets a value on the data source.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="client">Link with the gNMI client.</param>
		/// <param name="path">Path to perform the set on.</param>
		/// <param name="value">Value to be set.</param>
		public static void SetValue(SLProtocol protocol, GnmiClient client, Gnmi.Path path, string value)
		{
			try
			{
				client.Set(path, value);
			}
			catch (Exception ex)
			{
				protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|SetValue|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
			}
		}

		/// <summary>
		/// Fills in the connections for the second table that came in through subscription.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="value">Value that entered through subscription.</param>
		private static void FillSecondConnections(SLProtocol protocol, GnmiResponseValue value)
		{
			if (value.Value == null)
			{
				return;
			}

			ConnectionsPoll connections = JsonConvert.DeserializeObject<ConnectionsPoll>(value.Value.ToString());
			if (connections == null || connections.ConnectionList == null)
			{
				return;
			}

			Utilities.ConnectionTableCallbackManualPoll tableCallback = new Utilities.ConnectionTableCallbackManualPoll(protocol);
			tableCallback.FillConnections(connections.ConnectionList.MainConnection, connections.ConnectionList.SecondConnection);
		}
	}
}
