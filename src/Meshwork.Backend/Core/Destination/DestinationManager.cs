//
// DestinationManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System.Collections.Generic;
using System.Linq;

namespace Meshwork.Backend.Core.Destination
{
	public class DestinationManager
	{
	    private readonly Core core;
	    private readonly Dictionary<DestinationInfo, IDestination> destinationsFromSettings;
	    private readonly Dictionary<string, IDestinationSource>    sources;

		public DestinationManager (Core core)
		{
		    this.core = core;
		    sources = new Dictionary<string, IDestinationSource>();
			destinationsFromSettings = new Dictionary<DestinationInfo, IDestination>();

			SyncFromSettings();
		}

		public void RegisterSource (IDestinationSource source)
		{
			sources[source.DestinationType.ToString()] = source;

			source.DestinationAdded += source_DestinationAdded;
			source.DestinationRemoved += source_DestinationRemoved;

			foreach (var destination in source.Destinations) {
				source_DestinationAdded(destination);
			}
		}
		
		public void UnregisterSource (IDestinationSource source)
		{
			source.DestinationAdded -= source_DestinationAdded;
			source.DestinationRemoved -= source_DestinationRemoved;
			foreach (var destination in source.Destinations) {
				source_DestinationRemoved(destination);
			}
			sources.Remove(source.DestinationType.ToString());
		}
		
		public IDestinationSource[] Sources => sources.Values.ToArray();

	    public bool SupportsDestinationType (string typeName)
		{
			return sources.ContainsKey(typeName);
		}

		/// <summary>Returns a list of all local destinations.</summary>
		public IDestination[] Destinations {
			get {
				var result = new List<IDestination>();
				foreach (var source in sources.Values) {
					result.AddRange(source.Destinations);
				}
				foreach (var destination in destinationsFromSettings.Values) {
					result.Add(destination);
				}
				return result.ToArray();
			}
		}

		public DestinationInfo[] DestinationInfos {
			get {
			    return Destinations.Select(destination => destination.CreateDestinationInfo()).ToArray();
			}
		}

		private void source_DestinationAdded (IDestination destination)
		{

		}

		private void source_DestinationRemoved (IDestination destination)
		{

		}
		
		internal static IDestination[] GetConnectableDestinations (IDestination[] destinations)
		{
		    return destinations.Where(d => d.CanConnect).OrderByDescending(d => d).ToArray();
		}

		public void SyncFromSettings ()
		{
			// Remove old destinations
			var toRemove = destinationsFromSettings
				.Where(pair => !core.Settings.SavedDestinationInfos.Contains(pair.Key))
				.Select(pair => pair.Key)
				.ToList();
		    foreach (var info in toRemove) {
				destinationsFromSettings.Remove(info);
			}

			// Add new destinations
			foreach (var info in core.Settings.SavedDestinationInfos) {
				if (!destinationsFromSettings.ContainsKey(info)) {
					info.Local = true;
					var destination = info.CreateDestination(core);
					destinationsFromSettings[info] = destination;
				}
			}
		}
	}
}
