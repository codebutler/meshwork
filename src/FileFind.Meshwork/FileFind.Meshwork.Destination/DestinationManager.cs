//
// DestinationManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;

namespace FileFind.Meshwork.Destination
{
	public class DestinationManager
	{
		Dictionary<DestinationInfo, IDestination> destinationsFromSettings;
		Dictionary<string, IDestinationSource>    sources;

		public DestinationManager ()
		{
			sources = new Dictionary<string, IDestinationSource>();
			destinationsFromSettings = new Dictionary<DestinationInfo, IDestination>();

			SyncFromSettings();
		}

		public void RegisterSource (IDestinationSource source)
		{
			sources[source.DestinationType.ToString()] = source;

			source.DestinationAdded += source_DestinationAdded;
			source.DestinationRemoved += source_DestinationRemoved;

			foreach (IDestination destination in source.Destinations) {
				source_DestinationAdded(destination);
			}
		}

		public bool SupportsDestinationType (string typeName)
		{
			return sources.ContainsKey(typeName);
		}

		/// <summary>Returns a list of all local destinations.</summary>
		public IDestination[] Destinations {
			get {
				List<IDestination> result = new List<IDestination>();
				foreach (IDestinationSource source in sources.Values) {
					result.AddRange(source.Destinations);
				}
				foreach (IDestination destination in destinationsFromSettings.Values) {
					result.Add(destination);
				}
				return result.ToArray();
			}
		}

		public DestinationInfo[] DestinationInfos {
			get {
				List<DestinationInfo> result = new List<DestinationInfo>();

				foreach (IDestination destination in this.Destinations) {
					result.Add(destination.CreateDestinationInfo());
				}

				return result.ToArray();
			}
		}

		private void source_DestinationAdded (IDestination destination)
		{

		}

		private void source_DestinationRemoved (IDestination destination)
		{

		}

		public void SyncFromSettings ()
		{
			// Remove old destinations
			List<DestinationInfo> toRemove = new List<DestinationInfo>();
			foreach (KeyValuePair<DestinationInfo,IDestination> pair in destinationsFromSettings) {
				if (!Core.Settings.SavedDestinationInfos.Contains(pair.Key)) {
					toRemove.Add(pair.Key);
				}
			}
			foreach (DestinationInfo info in toRemove) {
				destinationsFromSettings.Remove(info);
			}

			// Add new destinations
			foreach (DestinationInfo info in Core.Settings.SavedDestinationInfos) {
				if (!destinationsFromSettings.ContainsKey(info)) {
					info.Local = true;
					IDestination destination = info.CreateDestination();
					destinationsFromSettings[info] = destination;
				}
			}
		}
	}
}
