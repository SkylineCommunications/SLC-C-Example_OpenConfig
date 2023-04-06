namespace QAction_399.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;
	using QAction_399.Models;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Models;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Protocol.DataMapper.Args;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// Helper class to be used when manually subscribing to the connections table.
	/// </summary>
	internal class ConnectionTableCallbackManualSubscribe : ConnectionTableCallback
	{
		private readonly object _lock;
		private readonly HashSet<string> _primaryKeyParameterValues;
		private readonly HashSet<string> _reportedPrimaryKeys;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionTableCallbackManualSubscribe"/> class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		public ConnectionTableCallbackManualSubscribe(SLProtocol protocol) : base(protocol)
		{
			_lock = new object();
			_primaryKeyParameterValues = new HashSet<string>();
			_reportedPrimaryKeys = new HashSet<string>();
		}

		/// <summary>
		/// Handle responses coming in via subscription.
		/// </summary>
		/// <param name="response">Response with changed values.</param>
		public void HandleIncomingResponseOpenflow(IEnumerable<GnmiResponseValue> response)
		{
			try
			{
				// Subscriptions enter through multiple threads, so locking is required.
				lock (_lock)
				{
					bool hasSyncResponse = false;
					foreach (var value in response)
					{
						if (value.IsSyncResponse)
						{
							hasSyncResponse = true;
							continue;
						}

						if (value.StringPath != "system/openflow/controllers/controller[name='second']/connections")
						{
							continue;
						}

						ConnectionsSubscription connections = JsonConvert.DeserializeObject<ConnectionsSubscription>(value.Value.ToString());
						ProcessConnectionsSubscribed(connections);
					}

					if (hasSyncResponse)
					{
						CleanOldKeys();
					}
				}
			}
			catch (Exception ex)
			{
				Protocol.Log("QA" + Protocol.QActionID + "|-1|HandleIncomingResponseOpenflow|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
			}
		}

		/// <summary>
		/// Reads out the existing primary keys on the parameter table.
		/// </summary>
		public void ReadPrimaryKeysSecondConnectionSubscribed()
		{
			lock (_lock)
			{
				object[] cols = (object[])Protocol.NotifyProtocol((int)Skyline.DataMiner.Net.Messages.NotifyType.NT_GET_TABLE_COLUMNS, Parameter.Openflowsecondcontrollerconnectionssubscribed.tablePid, new uint[] { Parameter.Openflowsecondcontrollerconnectionssubscribed.Idx.openflowsecondcontrollerconnectionssubscribedprimarykey });
				if (cols == null || cols.Length < 1)
				{
					return;
				}

				_primaryKeyParameterValues.Clear();
				object[] colPk = (object[])cols[0];
				for (int i = 0; i < colPk.Length; i++)
				{
					string pk = Convert.ToString(colPk[i]);
					if (!String.IsNullOrWhiteSpace(pk))
					{
						_primaryKeyParameterValues.Add(pk);
					}
				}
			}
		}

		/// <summary>
		/// Deletes the rows that were not reported during the subscription interval.
		/// </summary>
		private void CleanOldKeys()
		{
			HashSet<string> oldKeys = new HashSet<string>();
			foreach (string pk in _primaryKeyParameterValues)
			{
				if (!_reportedPrimaryKeys.Contains(pk))
				{
					oldKeys.Add(pk);
				}
			}

			_reportedPrimaryKeys.Clear();
			foreach (string pk in oldKeys)
			{
				_primaryKeyParameterValues.Remove(pk);
			}

			Protocol.DeleteRow(Parameter.Openflowsecondcontrollerconnectionssubscribed.tablePid, oldKeys.ToArray());
		}

		/// <summary>
		/// Creates the table row.
		/// </summary>
		/// <param name="pk">Primary key.</param>
		/// <param name="connectionState">Values of the connection.</param>
		/// <returns>Row to be added to the table.</returns>
		private QActionTableRow FillSecondConnectionSubscribedRow(string pk, ConfigState connectionState)
		{
			return new OpenflowsecondcontrollerconnectionssubscribedQActionRow
			{
				Openflowsecondcontrollerconnectionssubscribedprimarykey = pk,
				Openflowsecondcontrollerconnectionssubscribedauxiliaryid = connectionState.AuxId,
				Openflowsecondcontrollerconnectionssubscribedpriority = connectionState.Priority,
				Openflowsecondcontrollerconnectionssubscribedipaddress = connectionState.Address,
				Openflowsecondcontrollerconnectionssubscribedport = connectionState.Port,
				Openflowsecondcontrollerconnectionssubscribedtransportprotocol = ConvertTransportType(new DataMinerConnectorRawValueArgs { Value = connectionState.Transport }),
				Openflowsecondcontrollerconnectionssubscribedcertificateid = connectionState.CertificateId == null ? "-1" : connectionState.CertificateId,
				Openflowsecondcontrollerconnectionssubscribedsourceinterface = connectionState.SourceInterface,
				Openflowsecondcontrollerconnectionssubscribedstate = ConvertBoolType(new DataMinerConnectorRawValueArgs { Value = connectionState.Connected }),
				Openflowsecondcontrollerconnectionssubscribeddisplaykey = CreateKey(connectionState.AuxId, connectionState.Address, connectionState.Port),
			};
		}

		/// <summary>
		/// Processes the subscription value that entered.
		/// </summary>
		/// <param name="connections">Connections value that came in through subscription.</param>
		private void ProcessConnectionsSubscribed(ConnectionsSubscription connections)
		{
			if (connections == null || connections.ConnectionList == null)
			{
				return;
			}

			List<QActionTableRow> rows = new List<QActionTableRow>();
			foreach (var connection in connections.ConnectionList)
			{
				if (connection == null || connection.State == null)
				{
					continue;
				}

				string pk = Convert.ToString(connection.AuxId);
				_reportedPrimaryKeys.Add(pk);
				_primaryKeyParameterValues.Add(pk);
				rows.Add(FillSecondConnectionSubscribedRow(pk, connection.State));
			}

			QActionTable table = new QActionTable(Protocol, Parameter.Openflowsecondcontrollerconnectionssubscribed.tablePid, String.Empty);
			table.FillArrayNoDelete(rows);
		}
	}
}
