namespace QAction_399.ModelHelpers
{
	using System.Collections.Generic;
	using Skyline.DataMiner.Helper.OpenConfig.Api;
	using Skyline.DataMiner.Scripting;

	internal static class CapabilitiesHelper
	{
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

			protocol.SetParameter(Parameter.gnmiversion, capabilities.GNMIVersion);

			if (capabilities.SupportedModels == null || capabilities.SupportedModels.Count <= 0)
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
	}
}
