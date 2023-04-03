namespace QAction_399.Utilities
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Skyline.DataMiner.Helper.OpenConfig.Enums;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// The callback functions of the DataMinerConnectorDataGridColumn objects do not have an SLProtocol object. This class is an example on how the methods can be implemented with access to SLProtocol.
	/// </summary>
	internal class ConnectionTableCallback
	{
		private readonly SLProtocol _protocol;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionTableCallback"/> class.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		public ConnectionTableCallback(SLProtocol protocol)
		{
			_protocol = protocol;
		}

		/// <summary>
		/// Converts the raw value into a matching discreet parameter value.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public object ConvertTransportType(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			string error = "-2";
			try
			{
				string sVal = Convert.ToString(value);
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
				_protocol.Log("QA" + _protocol.QActionID + "|-1|ConvertTransportType|Exception thrown:" + Environment.NewLine + ex, LogType.Error, LogLevel.NoLogging);
				return error;
			}
		}

		/// <summary>
		/// Converts the raw value into a matching discreet parameter value.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public object ConvertBoolType(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			if (value is bool bValue)
			{
				return bValue ? "1" : "0";
			}

			return "-1";
		}

		/// <summary>
		/// Creates a new display key based on the other values. Used by the DataMapper.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="values">Mapping between the parameter ID and the parameter value.</param>
		/// <param name="timestamp">Timestamp of the raw values.</param>
		/// <returns>Created display key to be set on the parameter.</returns>
		public object CreateKey(DataValueOriginType origin, string pk, Dictionary<int, object> values, DateTime timestamp)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsauxiliaryid, out object oAuxId))
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
				stringBuilder.Append(pk);
			}

			stringBuilder.Append("-");
			if (values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsipaddress, out object address))
			{
				stringBuilder.Append(Convert.ToString(address));
			}
			else
			{
				stringBuilder.Append("N/A");
			}

			stringBuilder.Append(":");
			if (values.TryGetValue(Parameter.Openflowmaincontrollerconnections.Pid.openflowmaincontrollerconnectionsport, out object port))
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
		public string CreateKey(int auxId, string address, int port)
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
	}
}
