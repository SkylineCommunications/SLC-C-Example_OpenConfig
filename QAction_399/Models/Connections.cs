namespace QAction_399.Models
{
	using System.Collections.Generic;
	using Newtonsoft.Json;

	internal class ConnectionsPoll
	{
		[JsonProperty("connection")]
		public ConnectionItems ConnectionList { get; set; }
	}

	internal class ConnectionsSubscription
	{
		[JsonProperty("openconfig-openflow:connection")]
		public List<Connection> ConnectionList { get; set; }
	}

	internal class ConnectionItems
	{
		[JsonProperty("0")]
		public Connection MainConnection { get; set; }

		[JsonProperty("1")]
		public Connection SecondConnection { get; set; }
	}

	internal class Connection
	{
		[JsonProperty("aux-id")]
		public int AuxId { get; set; }

		[JsonProperty("config")]
		public ConfigState Config { get; set; }

		[JsonProperty("state")]
		public ConfigState State { get; set; }
	}

	internal class ConfigState
	{
		[JsonProperty("address")]
		public string Address { get; set; }

		[JsonProperty("aux-id")]
		public int AuxId { get; set; }

		[JsonProperty("certificate-id")]
		public string CertificateId { get; set; }

		[JsonProperty("connected")]
		public bool? Connected { get; set; }

		[JsonProperty("port")]
		public int Port { get; set; }

		[JsonProperty("priority")]
		public int Priority { get; set; }

		[JsonProperty("source-interface")]
		public string SourceInterface { get; set; }

		[JsonProperty("transport")]
		public string Transport { get; set; }
	}
}
