namespace QAction_399.Utilities
{
	using System;
	using System.Text;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Protocol.DataMapper.Args;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// The callback functions of the DataMinerConnectorDataGridColumn objects do not have an SLProtocol object. This class is an example on how the methods can be implemented with access to SLProtocol.
	/// </summary>
	internal class ConnectionTableCallback
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionTableCallback"/> class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		public ConnectionTableCallback(SLProtocol protocol)
		{
			Protocol = protocol;
		}

		/// <summary>
		/// Gets the protocol object from the constructor. Do not use this object to get to specific items like GetTriggerParameter().
		/// </summary>
		protected SLProtocol Protocol { get; }

		/// <summary>
		/// Converts the raw value into a matching discreet parameter value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static string ConvertBoolType(DataMinerConnectorRawValueArgs rawValue)
		{
			string error = "-1";
			if (rawValue == null)
			{
				return error;
			}

			if (rawValue.Value is bool bValue)
			{
				return bValue ? "1" : "0";
			}

			return error;
		}

		/// <summary>
		/// Creates a new display key based on the other values. Used by the DataMapper.
		/// </summary>
		/// <param name="triggerValues">Values that are part of the display key.</param>
		/// <returns>Created display key to be set on the parameter.</returns>
		public static string CreateKey(DataMinerConnectorTriggerValueArgs triggerValues)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (triggerValues.Values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsauxiliaryid, out object oAuxId))
			{
				string auxId = Convert.ToString(oAuxId);
				if (auxId == "0")
				{
					auxId = "Main";
				}

				stringBuilder.Append(auxId);
			}
			else
			{
				stringBuilder.Append(triggerValues.PrimaryKey);
			}

			stringBuilder.Append("-");
			if (triggerValues.Values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsipaddress, out object address))
			{
				stringBuilder.Append(Convert.ToString(address));
			}
			else
			{
				stringBuilder.Append("N/A");
			}

			stringBuilder.Append(":");
			if (triggerValues.Values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsport, out object port))
			{
				stringBuilder.Append(Convert.ToString(port));
			}
			else
			{
				stringBuilder.Append("N/A");
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Creates a new display key based on the other values. Used by manual processing.
		/// </summary>
		/// <param name="auxId">Auxiliary ID.</param>
		/// <param name="address">IP address.</param>
		/// <param name="port">TCP port.</param>
		/// <returns>Created display key to be set on the parameter.</returns>
		public static string CreateKey(int auxId, string address, int port)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (auxId == 0)
			{
				stringBuilder.Append("Main");
			}
			else
			{
				stringBuilder.Append(auxId.ToString());
			}

			stringBuilder.Append("-");
			if (String.IsNullOrEmpty(address))
			{
				stringBuilder.Append("N/A");
			}
			else
			{
				stringBuilder.Append(Convert.ToString(address));
			}

			stringBuilder.Append(":");
			if (port == 0)
			{
				stringBuilder.Append("N/A");
			}
			else
			{
				stringBuilder.Append(port.ToString());
			}

			return stringBuilder.ToString();
		}

		/// <summary>
		/// Converts the raw value into a matching discreet parameter value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public string ConvertTransportType(DataMinerConnectorRawValueArgs rawValue)
		{
			string error = "-2";
			if (rawValue == null)
			{
				return error;
			}

			try
			{
				string sVal = Convert.ToString(rawValue.Value);
				if (String.IsNullOrEmpty(sVal))
				{
					return error;
				}

				switch (sVal)
				{
					case "TCP": return "1";
					case "TLS": return "2";
					default: return error;
				}
			}
			catch (Exception ex)
			{
				Protocol.Log("QA" + Protocol.QActionID + "|-1|ConvertTransportType|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
				return error;
			}
		}
	}
}
