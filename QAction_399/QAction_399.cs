using System;
using System.Collections.Generic;
using QAction_399.ModelHelpers;
using QAction_399.Utilities;
using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Api;
using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Models;
using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Protocol.DataMapper;
using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Utils;
using Skyline.DataMiner.Scripting;

/// <summary>
/// DataMiner QAction Class.
/// </summary>
public sealed class QAction : IDisposable
{
	private const string OPENFLOW_SUBSCRIPTION_NAME = "openflow";
	private const string SYSTEM_SUBSCRIPTION_NAME = "system";

	private readonly object clientLock = new object();

	private readonly List<Gnmi.Path> systemGroup = new List<Gnmi.Path>();

	private readonly List<Gnmi.Path> openflowGroup = new List<Gnmi.Path>();

	private readonly List<Gnmi.Path> interfaceGroup = new List<Gnmi.Path>();

	private bool isDisposed;

	private bool isSubscribed;

	/// <summary>
	/// Indicates if the gNMI client is busy connecting.
	/// </summary>
	/// <remarks>Waiting on gNMI client connection takes 15s timeout when not available. Poll group is executed every 10s. Without this boolean there are threads starting at a higher rate than are finishing, which leads to a thread memory leak.</remarks>
	private bool isConnecting;

	private SLProtocol _protocol;

	/// <summary>
	/// The OpenConfig client, we retain until we dispose to avoid the connect overhead.
	/// </summary>
	private GnmiClient gnmiClient;

	private DataSourceConfiguration clientConfig;

	private DataMinerConnectorDataMapper dataMapper;

	/// <summary>
	/// Main QAction entry point.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	public void Run(SLProtocol protocol)
	{
		var trigger = protocol.GetTriggerParameter();
		_protocol = protocol;

		try
		{
			switch (trigger)
			{
				case Parameter.afterstartup_2:
					EnsureIsInitialized(protocol);
					break;
				case Parameter.pollunsubscribeddata_3:
					PollNotSubscribedData(protocol);
					break;
				case Parameter.Write.systemstateloginbanner_409:
				case Parameter.Write.systemstatemotdbanner_411:
					SetSystemValue(protocol, trigger);
					break;
				default:
					protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|Run|Unknown trigger:" + trigger, LogType.Error, LogLevel.NoLogging);
					break;
			}
		}
		catch (Exception e)
		{
			protocol.Log("QA" + protocol.QActionID + "|" + trigger + "|Run|Exception thrown:" + Environment.NewLine + e, LogType.Error, LogLevel.NoLogging);
		}
	}

	/// <summary>
	/// Disposing the QAction.
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Reads out the configuration to connect with the gNMI client.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	/// <returns>Configuration that is needed to connect.</returns>
	private static DataSourceConfiguration ReadConfiguration(SLProtocol protocol)
	{
		DataSourceConfiguration dataSourceConfiguration = new DataSourceConfiguration();

		if (!protocol.IsEmpty(Parameter.datasourceip))
		{
			dataSourceConfiguration.IpAddress = Convert.ToString(protocol.GetParameter(Parameter.datasourceip));
		}

		if (!protocol.IsEmpty(Parameter.datasourceport))
		{
			dataSourceConfiguration.Port = Convert.ToUInt32(protocol.GetParameter(Parameter.datasourceport));
		}

		if (!protocol.IsEmpty(Parameter.datasourceusername))
		{
			dataSourceConfiguration.UserName = Convert.ToString(protocol.GetParameter(Parameter.datasourceusername));
		}

		if (!protocol.IsEmpty(Parameter.datasourcepassword))
		{
			dataSourceConfiguration.Password = Convert.ToString(protocol.GetParameter(Parameter.datasourcepassword));
		}

		dataSourceConfiguration.ClientCertificate = Convert.ToString(protocol.GetParameter(Parameter.clientcertificate));

		return dataSourceConfiguration;
	}

	/// <summary>
	/// Disposing the QAction, this will dispose the internal gNMI client.
	/// </summary>
	/// <param name="disposing">Boolean indicating if the QAction is being disposed.</param>
	private void Dispose(bool disposing)
	{
		if (disposing && !isDisposed)
		{
			isDisposed = true;
			gnmiClient?.Dispose();
			gnmiClient = null;
		}
	}

	/// <summary>
	/// Verifies if the expected subscriptions were being done.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	private void EnsureIsInitialized(SLProtocol protocol)
	{
		try
		{
			if (isSubscribed || isConnecting)
			{
				return; // Is already subscribed so this thread can return without having to wait for the lock.
			}

			lock(clientLock)
			{
				if (isSubscribed || isConnecting)
				{
					return; // Subscription happened while waiting on lock so this thread can return.
				}

				ConnectionTableCallbackManualSubscribe tableCallback = new ConnectionTableCallbackManualSubscribe(protocol);
				tableCallback.ReadPrimaryKeysSecondConnectionSubscribed();
				TimeSpan sampleInterval = TimeSpan.FromSeconds(10); // expecting values back every 10s. An interval is needed to calculate a correct rate.
				var client = GetClient(protocol);
				client.Subscribe(SYSTEM_SUBSCRIPTION_NAME, new[] { dataMapper.GetPathForPid(Parameter.systemstatecurrentdatetime) });  // The datetime is the only value that updates on the Onos (once per second). Path is known by the DataMapper, parameter value will be filled in automatically and no further parsing will be needed.
				client.Subscribe(OPENFLOW_SUBSCRIPTION_NAME, sampleInterval, new[] { "system/openflow/controllers/controller[name='second']/connections" }, tableCallback.HandleIncomingResponseOpenflow); // Path is not known by the DataMapper, this will need to be parsed manually in HandleIncomingResponseOpenflow.

				// client.Subscribe("interfaces", sampleIntervalMs, interfaceGroup); // Note, the Onos throws an error when subscribing to this table, breaking communication.
				isSubscribed = true;
			}
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|EnsureIsInitialized|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
		}
	}

	/// <summary>
	/// Get the gNMI client, or sets up the connection if not done yet.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	/// <returns>gNMI client.</returns>
	private GnmiClient GetClient(SLProtocol protocol)
	{
		// Lock is needed because QAction is queued. When being triggered multiple times at the same moment, it would create multiple gnmiClients without the lock.
		lock (clientLock)
		{
			// We haven not attached the "change configuration" trigger yet, so we simply read it out and check if it has changed
			var config = ReadConfiguration(protocol);

			if (gnmiClient == null)
			{
				ILogger logger = new MiddleWareLogger(protocol);
				gnmiClient = new GnmiClient((uint)protocol.DataMinerID, (uint)protocol.ElementID, protocol.ElementName, config, logger);
				clientConfig = config;
				SetupDataMapper(protocol);
				gnmiClient.SetDataMapper(dataMapper);
				gnmiClient.ConnectionStateChanged += GnmiClientOnConnectionStateChanged;
			}
			else
			{
				VerifyConnectionConfiguration(config);
			}

			if (!gnmiClient.IsConnected)
			{
				protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|GetClient|Connecting...", LogType.Information, LogLevel.NoLogging);
				try
				{
					isConnecting = true;
					gnmiClient.Connect();
					isConnecting = false;
				}
				catch
				{
					isConnecting = false;
					throw;
				}
			}

			return gnmiClient;
		}
	}

	/// <summary>
	/// Changes the configuration on the gNMI client in case the settings changed.
	/// </summary>
	/// <param name="config">New configuration for the gNMI client.</param>
	private void VerifyConnectionConfiguration(DataSourceConfiguration config)
	{
		if (config.Equals(clientConfig))
		{
			return;
		}

		try
		{
			isConnecting = true; // Changing configuration means a reconnect internally.
			gnmiClient.ChangeConfiguration(config);
			clientConfig = config;
			isConnecting = false;
		}
		catch
		{
			isConnecting = false;
			throw;
		}
	}

	/// <summary>
	/// Called when the connection state of the gNMI client changes and adapts the element timeout state accordingly.
	/// </summary>
	/// <param name="sender">Sender that triggered the event.</param>
	/// <param name="e">Event arguments.</param>
	private void GnmiClientOnConnectionStateChanged(object sender, EventArgs e)
	{
		try
		{
			if (isDisposed)
			{
				return;
			}

			bool isConnected = gnmiClient.IsConnected;
			string previousConnectionState = Convert.ToString(_protocol.GetParameter(Parameter.grpcconnectionstate_22));
			if (!String.IsNullOrWhiteSpace(previousConnectionState))
			{
				bool wasConnected = previousConnectionState == "1";
				if (wasConnected == isConnected)
				{
					return;
				}
			}

			string setConnectionState = isConnected ? "1" : "0";
			_protocol.NotifyProtocol((int)Skyline.DataMiner.Net.Messages.NotifyType.NT_CHANGE_COMMUNICATION_STATE, isConnected, 99);
			_protocol.SetParameter(Parameter.grpcconnectionstate_22, setConnectionState);
		}
		catch (Exception ex)
		{
			_protocol.Log("QA" + _protocol.QActionID + "|-1|GnmiClientOnConnectionStateChanged|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
		}
	}

	/// <summary>
	/// Polls the data that is not subscribed.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	private void PollNotSubscribedData(SLProtocol protocol)
	{
		try
		{
			if (isConnecting)
			{
				return; // Another thread is already busy connecting. Returning.
			}

			var client = GetClient(protocol);
			CapabilitiesHelper.PollCapabilities(protocol, client);
			SystemHelper.PollSystemState(protocol, client, systemGroup);
			SystemHelper.PollOpenFlowDataMapper(protocol, client, openflowGroup);
			SystemHelper.PollOpenFlowManualParsing(protocol, client);
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|" + protocol.GetTriggerParameter() + "|PollNotSubscribedData|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
		}
	}

	/// <summary>
	/// The action for write parameters of system values.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	/// <param name="triggerParameterId">Id of the parameter that triggered this QAction.</param>
	private void SetSystemValue(SLProtocol protocol, int triggerParameterId)
	{
		try
		{
			var value = Convert.ToString(protocol.GetParameter(triggerParameterId));

			var client = GetClient(protocol);
			var path = dataMapper.GetPathForPid(triggerParameterId - 1);
			SystemHelper.SetValue(protocol, client, path, value);
			SystemHelper.PollSystemState(protocol, client, systemGroup);
		}
		catch (Exception ex)
		{
			protocol.Log("QA" + protocol.QActionID + "|" + triggerParameterId + "|SetSystemValue|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
		}
	}

	/// <summary>
	/// Sets up the data mapper. This contains the mapping between the parameter ID and the YANG model.
	/// </summary>
	/// <param name="protocol">Link with SLProtocol.</param>
	private void SetupDataMapper(SLProtocol protocol)
	{
		// Note, system/openflow/agent/config should be used for sets, while state is used to read, but the Onos simulation only has values on config for openflow
		ConnectionTableCallback tableCallback = new ConnectionTableCallback(protocol);
		string notAvailable = "-1";
		dataMapper = new DataMinerConnectorDataMapper(
			protocol,
			new List<IDataMinerConnectorDataEntity>
			{
				new DataMinerConnectorParameter("system/state/current-datetime", Parameter.systemstatecurrentdatetime_406) { OnRawValueChange = ValueConverter.ConvertDatetimeStringValue },
				new DataMinerConnectorParameter("system/state/login-banner", Parameter.systemstateloginbanner_408),
				new DataMinerConnectorParameter("system/state/motd-banner", Parameter.systemstatemotdbanner_410),
				new DataMinerConnectorParameter("system/openflow/agent/config/datapath-id", Parameter.openflowdatapathid_3000),
				new DataMinerConnectorParameter("system/openflow/agent/config/failure-mode", Parameter.openflowfailuremode_3001) { OnRawValueChange = ValueConverter.ConvertToFailureModeStateEnumValue },
				new DataMinerConnectorParameter("system/openflow/agent/config/backoff-interval", Parameter.openflowbackoffinterval_3002),
				new DataMinerConnectorParameter("system/openflow/agent/config/max-backoff", Parameter.openflowmaxbackoff_3003),
				new DataMinerConnectorParameter("system/openflow/agent/config/inactivity-probe", Parameter.openflowinactivityprobeperiod_3004),
				new DataMinerConnectorDataGrid("system/openflow/controllers/controller[name='main']/connections/openconfig-openflow:connection<aux-id>", Parameter.Openflowmaincontrollerconnections.tablePid, new List<IDataMinerConnectorDataGridColumn>
				{
					new DataMinerConnectorDataGridColumn("state/aux-id", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsauxiliaryid, notAvailable),
					new DataMinerConnectorDataGridColumn("state/priority", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionspriority, notAvailable),
					new DataMinerConnectorDataGridColumn("state/address", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsipaddress, notAvailable),
					new DataMinerConnectorDataGridColumn("state/port", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsport, notAvailable),
					new DataMinerConnectorDataGridColumn("state/transport", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionstransportprotocol, notAvailable) { OnRawValueChange = tableCallback.ConvertTransportType },
					new DataMinerConnectorDataGridColumn("state/certificate-id", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionscertificateid, notAvailable),
					new DataMinerConnectorDataGridColumn("state/source-interface", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionssourceinterface, notAvailable),
					new DataMinerConnectorDataGridColumn("state/connected", Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsstate, notAvailable) { OnRawValueChange = ConnectionTableCallback.ConvertBoolType },
					new DataMinerConnectorDataGridColumn(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsdisplaykey, notAvailable)
					{
						OnTriggerValueChange = ConnectionTableCallback.CreateKey,
						TriggerColumnParameterIds = new List<int>
						{
							Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsauxiliaryid,
							Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsipaddress,
							Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsport,
						},
					},
				}),
				new DataMinerConnectorDataGrid("interfaces/interface/state", Parameter.Interfacesstate.tablePid, new List<IDataMinerConnectorDataGridColumn>
				{
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:type", Parameter.Interfacesstate.Pid.interfacesstatetype, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:mtu", Parameter.Interfacesstate.Pid.interfacesstatemtu, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:loopback-mode", Parameter.Interfacesstate.Pid.interfacesstateloopbackmode, notAvailable) { OnRawValueChange = ValueConverter.ConvertBoolToNumber },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:description", Parameter.Interfacesstate.Pid.interfacesstatedescription, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:enabled", Parameter.Interfacesstate.Pid.interfacesstatestate, notAvailable) { OnRawValueChange = ValueConverter.ConvertBoolToNumber },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:ifindex", Parameter.Interfacesstate.Pid.interfacesstateifindex, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:admin-status", Parameter.Interfacesstate.Pid.interfacesstateadminstatus, notAvailable) { OnRawValueChange = ValueConverter.ConvertToAdminStateEnumValue },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:oper-status", Parameter.Interfacesstate.Pid.interfacesstateoperstatus, notAvailable) { OnRawValueChange = ValueConverter.ConvertToOperStateEnumValue },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:last-change", Parameter.Interfacesstate.Pid.interfacesstatelastchange, notAvailable) { OnRawValueChange = ValueConverter.ConvertEpochTimeUtcTicksToOadate },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:logical", Parameter.Interfacesstate.Pid.interfacesstatelogical, notAvailable) { OnRawValueChange = ValueConverter.ConvertBoolToNumber },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-octets", Parameter.Interfacesstate.Pid.interfacesstateinoctets, notAvailable) { RateCalculator = ValueConverter.CustomBitRates, RateColumnParameterId = Parameter.Interfacesstate.Pid.interfacesstateinbitrate },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-pkts", Parameter.Interfacesstate.Pid.interfacesstateinpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-unicast-pkts", Parameter.Interfacesstate.Pid.interfacesstateinunicastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-broadcast-pkts", Parameter.Interfacesstate.Pid.interfacesstateinbroadcastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-multicast-pkts", Parameter.Interfacesstate.Pid.interfacesstateinmulticastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-discards", Parameter.Interfacesstate.Pid.interfacesstateindiscards, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-errors", Parameter.Interfacesstate.Pid.interfacesstateinerrors, notAvailable) { RateCalculator = ValueConverter.CustomErrorRates, RateColumnParameterId = Parameter.Interfacesstate.Pid.interfacesstateinerrorrate },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-unknown-protos", Parameter.Interfacesstate.Pid.interfacesstateinunknownprotos, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/in-fcs-errors", Parameter.Interfacesstate.Pid.interfacesstateinfcserrors, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-broadcast-pkts", Parameter.Interfacesstate.Pid.interfacesstateoutbroadcastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-discards", Parameter.Interfacesstate.Pid.interfacesstateoutdiscards, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-errors", Parameter.Interfacesstate.Pid.interfacesstateouterrors, notAvailable) { RateCalculator = ValueConverter.CustomErrorRates, RateColumnParameterId = Parameter.Interfacesstate.Pid.interfacesstateouterrorrate },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-multicast-pkts", Parameter.Interfacesstate.Pid.interfacesstateoutmulticastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-octets", Parameter.Interfacesstate.Pid.interfacesstateoutoctets, notAvailable) { RateCalculator = ValueConverter.CustomBitRates, RateColumnParameterId = Parameter.Interfacesstate.Pid.interfacesstateoutbitrate },
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-pkts", Parameter.Interfacesstate.Pid.interfacesstateoutpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/out-unicast-pkts", Parameter.Interfacesstate.Pid.interfacesstateoutunicastpckts, notAvailable),
					new DataMinerConnectorDataGridColumn("openconfig-interfaces:counters/last-clear", Parameter.Interfacesstate.Pid.interfacesstatelastclear, notAvailable) { OnRawValueChange = ValueConverter.ConvertEpochTimeUtcTicksToOadate },
					new DataMinerConnectorDataGridColumn(Parameter.Interfacesstate.Pid.interfacesstatedisplaykey, notAvailable) { OnTriggerValueChange = ValueConverter.CreateDisplayKey, TriggerColumnParameterIds = new List<int> { Parameter.Interfacesstate.Pid.interfacesstatedescription } },
					new DataMinerConnectorDataGridColumn(Parameter.Interfacesstate.Pid.interfacesstateinbitrate, notAvailable),
					new DataMinerConnectorDataGridColumn(Parameter.Interfacesstate.Pid.interfacesstateoutbitrate, notAvailable),
					new DataMinerConnectorDataGridColumn(Parameter.Interfacesstate.Pid.interfacesstateinerrorrate, notAvailable),
					new DataMinerConnectorDataGridColumn(Parameter.Interfacesstate.Pid.interfacesstateouterrorrate, notAvailable),
				}),
			});

		// Constructing group paths. This is done once, so there is no constant GetPathForPid needed when polling.
		systemGroup.Clear();
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.systemstateloginbanner));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.systemstatemotdbanner));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.openflowdatapathid));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.openflowfailuremode));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.openflowbackoffinterval));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.openflowmaxbackoff));
		systemGroup.Add(dataMapper.GetPathForPid(Parameter.openflowinactivityprobeperiod));

		openflowGroup.Clear();
		openflowGroup.Add(dataMapper.GetPathForPid(Parameter.Openflowmaincontrollerconnections.tablePid));

		interfaceGroup.Clear();
		interfaceGroup.Add(dataMapper.GetPathForPid(Parameter.Interfacesstate.tablePid));
	}
}
