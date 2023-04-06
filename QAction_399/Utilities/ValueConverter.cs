namespace QAction_399.ModelHelpers
{
	using System;
	using System.Collections.Generic;
	using QAction_399.Models;
	using QAction_399.Utilities;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Protocol.DataMapper.Args;
	using Skyline.DataMiner.Scripting;

	/// <summary>
	/// Helper class that is used to convert the raw values from the data source into a value that can be set on a parameter.
	/// </summary>
	internal class ValueConverter
	{
		private static readonly Dictionary<string, OperState> OperStateMap = new Dictionary<string, OperState> { { "UP", OperState.Up }, { "DOWN", OperState.Down }, { "TESTING", OperState.Testing }, { "UNKNOWN", OperState.Unknown }, { "DORMANT", OperState.Dormant }, { "NOT_PRESENT", OperState.NotPresent }, { "LOWER_LAYER_DOWN", OperState.LowerLayerDown } };
		private static readonly Dictionary<string, AdminState> AdminStateMap = new Dictionary<string, AdminState> { { "UP", AdminState.Up }, { "DOWN", AdminState.Down }, { "TESTING", AdminState.Testing } };
		private static readonly Dictionary<string, FailureModeState> FailureModeStateMap = new Dictionary<string, FailureModeState> { { "SECURE", FailureModeState.Secure }, { "STANDALONE", FailureModeState.Standalone } };

		private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		private static readonly int NotAvailable = -1;

		/// <summary>
		/// Converts the operator state into a discreet value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToOperStateEnumValue(DataMinerConnectorRawValueArgs rawValue)
		{
			if (rawValue != null && rawValue.Value is string sValue && OperStateMap.TryGetValue(sValue, out OperState state))
			{
				int iState = (int)state;
				return iState;
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the administrator state into a discreet value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToAdminStateEnumValue(DataMinerConnectorRawValueArgs rawValue)
		{
			if (rawValue != null && rawValue.Value is string sValue && AdminStateMap.TryGetValue(sValue, out AdminState state))
			{
				int iState = (int)state;
				return iState;
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the failure mode state into a discreet value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToFailureModeStateEnumValue(DataMinerConnectorRawValueArgs rawValue)
		{
			if (rawValue != null && rawValue.Value is string sValue && FailureModeStateMap.TryGetValue(sValue, out FailureModeState state))
			{
				int iState = (int)state;
				return iState;
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the epoch time into a date time value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertEpochTimeUtcTicksToOadate(DataMinerConnectorRawValueArgs rawValue)
		{
			if (rawValue != null && rawValue.Value is ulong ticks)
			{
				double secondsSinceEpoch;

				if (ticks > 946684800000000000)
				{
					secondsSinceEpoch = ticks / 1000000000d;
				}
				else
				{
					secondsSinceEpoch = ticks / 100d;
				}

				double convertedDate = Epoch.AddSeconds(secondsSinceEpoch).ToOADate();

				return convertedDate;
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the date time string from the data source into a valid parameter value.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Value in the correct format.</returns>
		public static object ConvertDatetimeStringValue(DataMinerConnectorRawValueArgs rawValue)
		{
			double emptyValue = 0.0;
			if (rawValue == null)
			{
				return emptyValue;
			}

			string rawDate = Convert.ToString(rawValue.Value);
			if (String.IsNullOrEmpty(rawDate))
			{
				return emptyValue;
			}

			if (DateTime.TryParseExact(rawDate, "yyyy-MM-ddTHH:mm:ssZzzz", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime convertedTime))
			{
				return convertedTime.ToOADate();
			}

			return emptyValue;
		}

		/// <summary>
		/// Creates the display key based on the incoming values.
		/// </summary>
		/// <param name="triggerValues">Values that are part of the display key.</param>
		/// <returns>Display key to be set on the parameter.</returns>
		public static string CreateDisplayKey(DataMinerConnectorTriggerValueArgs triggerValues)
		{
			string description;
			if (triggerValues.Values.TryGetValue(Parameter.Interfacesstate.Pid.interfacesstatedescription, out object descriptionVal))
			{
				description = Convert.ToString(descriptionVal);
			}
			else
			{
				description = String.Empty;
			}

			if (String.IsNullOrEmpty(description) || description == "-1")
			{
				return triggerValues.PrimaryKey;
			}

			return DisplayKeyUtil.GetDisplayKey(triggerValues.PrimaryKey, description);
		}

		/// <summary>
		/// Custom implementation of the bit rate calculator.
		/// </summary>
		/// <param name="rateValues">Values that are needed to calculate the rate on.</param>
		/// <returns>Calculated rate.</returns>
		public static object CustomBitRates(DataMinerConnectorRateArgs rateValues)
		{
			if (rateValues == null)
			{
				return NotAvailable;
			}

			if (rateValues.CurrentTimestampUtc <= rateValues.PreviousTimestampUtc || !UInt64.TryParse(Convert.ToString(rateValues.CurrentValue), out ulong uiNewValue) || !UInt64.TryParse(Convert.ToString(rateValues.PreviousValue), out ulong uiOldValue) || uiOldValue > uiNewValue)
			{
				return NotAvailable;
			}

			TimeSpan ts = rateValues.CurrentTimestampUtc - rateValues.PreviousTimestampUtc;
			double rate = 8.0 * Convert.ToDouble(uiNewValue - uiOldValue) / ts.TotalSeconds;
			return rate;
		}

		/// <summary>
		/// Custom implementation of the error rate calculator.
		/// </summary>
		/// <param name="rateValues">Values that are needed to calculate the rate on.</param>
		/// <returns>Calculated rate.</returns>
		public static object CustomErrorRates(DataMinerConnectorRateArgs rateValues)
		{
			if (rateValues == null)
			{
				return NotAvailable;
			}

			if (rateValues.CurrentTimestampUtc <= rateValues.PreviousTimestampUtc || !UInt64.TryParse(Convert.ToString(rateValues.CurrentValue), out ulong uiNewValue) || !UInt64.TryParse(Convert.ToString(rateValues.PreviousValue), out ulong uiOldValue) || uiOldValue > uiNewValue)
			{
				return NotAvailable;
			}

			TimeSpan ts = rateValues.CurrentTimestampUtc - rateValues.PreviousTimestampUtc;
			double rate = Convert.ToDouble(uiNewValue - uiOldValue) / ts.TotalSeconds;
			return rate;
		}

		/// <summary>
		/// Converts a boolean into a number.
		/// </summary>
		/// <param name="rawValue">Raw value that enters from data source.</param>
		/// <returns>Value in the correct format.</returns>
		public static object ConvertBoolToNumber(DataMinerConnectorRawValueArgs rawValue)
		{
			if (rawValue == null)
			{
				return NotAvailable;
			}

			if (rawValue.Value is bool bValue)
			{
				return Convert.ToDouble(bValue);
			}

			return NotAvailable;
		}
	}
}
