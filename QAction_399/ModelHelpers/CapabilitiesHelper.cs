namespace QAction_399.ModelHelpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.DataSources.OpenConfig.Gnmi.Api;
	using Skyline.DataMiner.Scripting;

	internal static class CapabilitiesHelper
	{
		private enum SupportedEncodingState
		{
			NotSupported = 0,
			Supported = 1,
		}

		/// <summary>
		/// Polls the capabilities and fills in the parameter table.
		/// </summary>
		/// <param name="protocol">Link with SLProtocol.</param>
		/// <param name="client">gNMI client.</param>
		public static void PollCapabilities(SLProtocol protocol, GnmiClient client)
		{
			// The GetCapabilities doesn't have a wrapping method (yet), but the result is quite easy to interpret.
			var capabilities = client.Capabilities();

			if (capabilities == null)
			{
				return;
			}

			Dictionary<int, string> currentParameterValues = GetCurrentParameterValues(protocol);
			Dictionary<int, object> setParameterValues = new Dictionary<int, object>
			{
				{ Parameter.gnmiversion, capabilities.GNMIVersion },
			};

			bool hasVersionChange = currentParameterValues.TryGetValue(Parameter.gnmiversion, out var currentVersion) && currentVersion != capabilities.GNMIVersion;
			FillSupportedEncodings(protocol, capabilities, currentParameterValues, setParameterValues, hasVersionChange);
			FillSupportedModels(protocol, capabilities);
		}

		private static void FillSupportedEncodings(SLProtocol protocol, Gnmi.CapabilityResponse capabilities, Dictionary<int, string> currentParameterValues, Dictionary<int, object> setParameterValues, bool hasVersionChange)
		{
			if (capabilities.SupportedEncodings == null || !capabilities.SupportedEncodings.Any())
			{
				if (hasVersionChange)
				{
					protocol.SetParameter(Parameter.gnmiversion, capabilities.GNMIVersion);
				}

				return;
			}

			SupportedEncodingState asciiEncoding = SupportedEncodingState.NotSupported;
			SupportedEncodingState bytesEncoding = SupportedEncodingState.NotSupported;
			SupportedEncodingState jsonEncoding = SupportedEncodingState.NotSupported;
			SupportedEncodingState jsonIetfEncoding = SupportedEncodingState.NotSupported;
			SupportedEncodingState protoEncoding = SupportedEncodingState.NotSupported;
			foreach (var supportedEncoding in capabilities.SupportedEncodings)
			{
				switch (supportedEncoding)
				{
					case Gnmi.Encoding.Ascii:
						asciiEncoding = SupportedEncodingState.Supported;
						break;
					case Gnmi.Encoding.Bytes:
						bytesEncoding = SupportedEncodingState.Supported;
						break;
					case Gnmi.Encoding.Json:
						jsonEncoding = SupportedEncodingState.Supported;
						break;
					case Gnmi.Encoding.JsonIetf:
						jsonIetfEncoding = SupportedEncodingState.Supported;
						break;
					case Gnmi.Encoding.Proto:
						protoEncoding = SupportedEncodingState.Supported;
						break;
					default:
						continue;
				}
			}

			bool hasAsciiChange = CompareValues(Parameter.asciiencoding, asciiEncoding, currentParameterValues, setParameterValues);
			bool hasBytesChange = CompareValues(Parameter.bytesencoding, bytesEncoding, currentParameterValues, setParameterValues);
			bool hasJsonChange = CompareValues(Parameter.jsonencoding, jsonEncoding, currentParameterValues, setParameterValues);
			bool hasJsonIetfChange = CompareValues(Parameter.jsonietfencoding, jsonIetfEncoding, currentParameterValues, setParameterValues);
			bool hasProtoChange = CompareValues(Parameter.protoencoding, protoEncoding, currentParameterValues, setParameterValues);
			bool hasFirstPartChange = hasAsciiChange || hasBytesChange || hasJsonChange || hasJsonIetfChange;
			if (hasFirstPartChange || hasProtoChange || hasVersionChange)
			{
				protocol.SetParameters(setParameterValues.Keys.ToArray(), setParameterValues.Values.ToArray());
			}
		}

		private static bool CompareValues(int parameterId, SupportedEncodingState encodingState, Dictionary<int, string> currentParameterValues, Dictionary<int, object> setParameterValues)
		{
			setParameterValues[parameterId] = Convert.ToString((int)encodingState);
			return currentParameterValues.TryGetValue(parameterId, out var currentValue) && currentValue == Convert.ToString((int)encodingState);
		}

		private static void FillSupportedModels(SLProtocol protocol, Gnmi.CapabilityResponse capabilities)
		{
			if (capabilities.SupportedModels == null || !capabilities.SupportedModels.Any())
			{
				return;
			}

			var rows = new List<QActionTableRow>();

			int pk = 1;
			foreach (var supportedModel in capabilities.SupportedModels)
			{
				CapabilitiesmodelstableQActionRow row = new CapabilitiesmodelstableQActionRow
				{
					Capabilitiesmodelstablekey = pk.ToString(),
					Capabilitiesmodelstablename = supportedModel.Name,
					Capabilitiesmodelstableorganization = supportedModel.Organization,
					Capabilitiesmodelstableversion = supportedModel.Version,
				};

				rows.Add(row);
				pk++;
			}

			QActionTable capabilitiesmodelsTable = new QActionTable(protocol, Parameter.Capabilitiesmodelstable.tablePid, "Supported Models");
			capabilitiesmodelsTable.FillArray(rows);
		}

		private static Dictionary<int, string> GetCurrentParameterValues(SLProtocol protocol)
		{
			Dictionary<int, string> values = new Dictionary<int, string>();
			uint[] paramIds = new uint[]
			{
				Parameter.gnmiversion,
				Parameter.asciiencoding,
				Parameter.bytesencoding,
				Parameter.jsonencoding,
				Parameter.jsonietfencoding,
				Parameter.protoencoding,
			};

			object[] parameterValues = (object[])protocol.GetParameters(paramIds);
			if (parameterValues == null || parameterValues.Length != paramIds.Length)
			{
				return values;
			}

			for (int i = 0; i < paramIds.Length; i++)
			{
				values[Convert.ToInt32(paramIds[i])] = Convert.ToString(parameterValues[i]);
			}

			return values;
		}
	}
}
