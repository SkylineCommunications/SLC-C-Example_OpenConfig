namespace QAction_399.ModelHelpers
{
	using System;
	using System.Collections.Generic;
	using QAction_399.Models;
	using QAction_399.Utilities;
	using Skyline.DataMiner.Helper.OpenConfig.Enums;
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
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToOperStateEnumValue(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			if (value is string sValue)
			{
				OperState state;
				if (OperStateMap.TryGetValue(sValue, out state))
				{
					int iState = (int)state;
					return iState;
				}
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the administrator state into a discreet value.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToAdminStateEnumValue(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			if (value is string sValue)
			{
				AdminState state;
				if (AdminStateMap.TryGetValue(sValue, out state))
				{
					int iState = (int)state;
					return iState;
				}
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the failure mode state into a discreet value.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertToFailureModeStateEnumValue(DataValueOriginType origin, object value, DateTime timestamp)
		{
			if (value is string sValue)
			{
				FailureModeState state;
				if (FailureModeStateMap.TryGetValue(sValue, out state))
				{
					int iState = (int)state;
					return iState;
				}
			}

			return NotAvailable;
		}

		/// <summary>
		/// Converts the epoch time into a date time value.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Raw value from the data source.</param>
		/// <param name="timestamp">Timestamp of the raw value.</param>
		/// <returns>Converted value to be set on the parameter.</returns>
		public static object ConvertEpochTimeUtcTicksToOadate(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			if (value is ulong ticks)
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
		/// <param name="origin">Indication where the data originated from.</param>
		/// <param name="value">Datetime value.</param>
		/// <param name="timestamp">Timestamp of when the value was polled.</param>
		/// <returns>Value in the correct format.</returns>
		public static object ConvertDatetimeStringValue(DataValueOriginType origin, object value, DateTime timestamp)
		{
			string rawDate = Convert.ToString(value);
			if (String.IsNullOrEmpty(rawDate))
			{
				return 0.0;
			}

			if (DateTime.TryParseExact(rawDate, "yyyy-MM-ddTHH:mm:ssZzzz", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime convertedTime))
			{
				return convertedTime.ToOADate();
			}

			return 0.0;
		}

		/// <summary>
		/// Creates the display key based on the incoming values.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="values">Mapping between the parameter ID and the parameter value.</param>
		/// <param name="timestamp">Timestamp of the values.</param>
		/// <returns>Display key to be set on the parameter.</returns>
		public static object CreateDisplayKey(DataValueOriginType origin, string pk, Dictionary<int, object> values, DateTime timestamp)
		{
			string description;
			if (values.TryGetValue(Parameter.Interfacesstate.Pid.interfacesstatedescription, out object descriptionVal))
			{
				description = Convert.ToString(descriptionVal);
			}
			else
			{
				description = String.Empty;
			}

			if (String.IsNullOrEmpty(description) || description == "-1")
			{
				return pk;
			}

			return DisplayKeyUtil.GetDisplayKey(pk, description);
		}

		/// <summary>
		/// Custom implementation of the bit rate calculator.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="newValue">New value.</param>
		/// <param name="newTime">Timestamp of the new value.</param>
		/// <param name="oldValue">Previous value.</param>
		/// <param name="oldTime">Timestamp of the previous value.</param>
		/// <returns>Calculated rate.</returns>
		public static object CustomBitRates(DataValueOriginType origin, string pk, object newValue, DateTime newTime, object oldValue, DateTime oldTime)
		{
			if (newTime <= oldTime || !UInt64.TryParse(Convert.ToString(newValue), out ulong uiNewValue) || !UInt64.TryParse(Convert.ToString(oldValue), out ulong uiOldValue) || uiOldValue > uiNewValue)
			{
				return -1;
			}

			TimeSpan ts = newTime - oldTime;
			double rate = 8.0 * Convert.ToDouble(uiNewValue - uiOldValue) / ts.TotalSeconds;
			return rate;
		}

		/// <summary>
		/// Custom implementation of the error rate calculator.
		/// </summary>
		/// <param name="origin">Indicates where the value originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="newValue">New value.</param>
		/// <param name="newTime">Timestamp of the new value.</param>
		/// <param name="oldValue">Previous value.</param>
		/// <param name="oldTime">Timestamp of the previous value.</param>
		/// <returns>Calculated rate.</returns>
		public static object CustomErrorRates(DataValueOriginType origin, string pk, object newValue, DateTime newTime, object oldValue, DateTime oldTime)
		{
			if (newTime <= oldTime || !UInt64.TryParse(Convert.ToString(newValue), out ulong uiNewValue) || !UInt64.TryParse(Convert.ToString(oldValue), out ulong uiOldValue) || uiOldValue > uiNewValue)
			{
				return -1;
			}

			TimeSpan ts = newTime - oldTime;
			double rate = Convert.ToDouble(uiNewValue - uiOldValue) / ts.TotalSeconds;
			return rate;
		}

		/// <summary>
		/// Converts a boolean into a number.
		/// </summary>
		/// <param name="origin">Indication where the data originated from.</param>
		/// <param name="pk">Primary key of the row.</param>
		/// <param name="value">Boolean value.</param>
		/// <param name="timestamp">Timestamp of when the value was polled.</param>
		/// <returns>Value in the correct format.</returns>
		public static object ConvertBoolToNumber(DataValueOriginType origin, string pk, object value, DateTime timestamp)
		{
			if (value is bool bValue)
			{
				return Convert.ToDouble(bValue);
			}

			return NotAvailable;
		}
	}
}
