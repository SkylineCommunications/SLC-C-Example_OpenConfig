namespace QAction_399.Utilities
{
	using System;
	using System.Collections.Generic;
	using QAction_399.Models;
	using Skyline.DataMiner.Helper.OpenConfig.Enums;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// Helper class to be used when manually polling the connections table.
	/// </summary>
	internal class ConnectionTableCallbackManualPoll : ConnectionTableCallback
	{
		private readonly SLProtocol _protocol;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionTableCallbackManualPoll"/> class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		public ConnectionTableCallbackManualPoll(SLProtocol protocol) : base(protocol)
		{
			_protocol = protocol;
		}

		/// <summary>
		/// Fills the table based on the connections.
		/// </summary>
		/// <param name="mainConnection">Main connection values.</param>
		/// <param name="secondConnection">Second connection values.</param>
		public void FillConnections(Connection mainConnection, Connection secondConnection)
		{
			List<QActionTableRow> rows = new List<QActionTableRow>();
			if (mainConnection != null && mainConnection.State != null)
			{
				rows.Add(FillSecondConnectionPolledRow(Convert.ToString(mainConnection.AuxId), mainConnection.State));
			}

			if (secondConnection != null && secondConnection.State != null)
			{
				rows.Add(FillSecondConnectionPolledRow(Convert.ToString(secondConnection.AuxId), secondConnection.State));
			}

			if (rows.Count == 0)
			{
				return;
			}

			QActionTable table = new QActionTable(_protocol, Parameter.Openflowsecondcontrollerconnectionspolled.tablePid, String.Empty);
			table.FillArray(rows);
		}

		/// <summary>
		/// Creates the table row.
		/// </summary>
		/// <param name="pk">Primary key.</param>
		/// <param name="connectionState">Values of the connection.</param>
		/// <returns>Row to be added to the table.</returns>
		private QActionTableRow FillSecondConnectionPolledRow(string pk, ConfigState connectionState)
		{
			return new OpenflowsecondcontrollerconnectionspolledQActionRow
			{
				Openflowsecondcontrollerconnectionspolledpk = pk,
				Openflowsecondcontrollerconnectionspolledauxiliaryid = connectionState.AuxId,
				Openflowsecondcontrollerconnectionspolledpriority = connectionState.Priority,
				Openflowsecondcontrollerconnectionspolledipaddress = connectionState.Address,
				Openflowsecondcontrollerconnectionspolledport = connectionState.Port,
				Openflowsecondcontrollerconnectionspolledtransport = ConvertTransportType(DataValueOriginType.Poll, pk, connectionState.Transport, DateTime.UtcNow),
				Openflowsecondcontrollerconnectionspolledcertificateid = connectionState.CertificateId == null ? "-1" : connectionState.CertificateId,
				Openflowsecondcontrollerconnectionspolledsourceinterface = connectionState.SourceInterface,
				Openflowsecondcontrollerconnectionspolledstate = ConvertBoolType(DataValueOriginType.Poll, pk, connectionState.Connected, DateTime.UtcNow),
				Openflowsecondcontrollerconnectionspolleddisplaykey = CreateKey(connectionState.AuxId, connectionState.Address, connectionState.Port),
			};
		}
	}
}
