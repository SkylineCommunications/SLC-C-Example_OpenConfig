namespace QAction_399.Utilities
{
	using System;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Helpers class that is used to create the display key for the interfaces table.
	/// </summary>
	internal static class DisplayKeyUtil
	{
		/// <summary>
		/// Creates the display key based on the incoming values.
		/// </summary>
		/// <param name="name">Name of the interface.</param>
		/// <param name="description">Description of the interface.</param>
		/// <returns>The created display key.</returns>
		public static string GetDisplayKey(string name, string description)
		{
			string displayKey = ExpandName(name) + "/" + description;

			return displayKey;
		}

		/// <summary>
		/// Expands the name into a more readable name.
		/// </summary>
		/// <param name="name">Name of the interface.</param>
		/// <returns>Expanded name.</returns>
		private static string ExpandName(string name)
		{
			Match m = Regex.Match(name, @"^(?<tag>(lo|eth))\d+(\/\d+)*$");

			if (m.Success)
			{
				string tag = m.Groups["tag"].Value;

				string expandedTag = String.Empty;

				switch (tag)
				{
					case "eth":
						expandedTag = "Ethernet";
						break;
					case "lo":
						expandedTag = "loopback";
						break;
					default:
						// Do nothing.
						break;
				}

				string expandedName = name.Replace(tag, expandedTag);

				return expandedName;
			}
			else
			{
				return name;
			}
		}
	}
}
