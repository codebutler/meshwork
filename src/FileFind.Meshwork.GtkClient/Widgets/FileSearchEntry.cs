//
// FileSearchEntry.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using Gtk;
using Banshee.Widgets;

namespace FileFind.Meshwork.GtkClient
{
	public class FileSearchEntry : SearchEntry
	{
		Dictionary<int,string> networkIDs = new Dictionary<int,string>();

		public FileSearchEntry ()
		{
			base.EmptyMessage = "Search for files";
			base.WidthRequest = 200;
			base.AddFilterOption(0, "All Networks");
			base.AddFilterSeparator();
			base.Activated += searchEntry_Activated;
			base.FilterChanged += searchEntry_FilterChanged;

			Core.NetworkAdded += Core_NetworkAdded;

			foreach (Network network in Core.Networks) {
				Core_NetworkAdded(network);
			}
		}

		public new void Activate ()
		{
			searchEntry_Activated(this, EventArgs.Empty);
		}

		private void searchEntry_Activated (object sender, EventArgs args)
		{
			try {
				if (base.ActiveFilterID > 0) {
					Core.FileSearchManager.NewFileSearch(base.Query, networkIDs[base.ActiveFilterID]);
				} else {
					Core.FileSearchManager.NewFileSearch(base.Query, null);
				}
			} catch (Exception ex) {
				Gui.ShowErrorDialog(ex.Message);
			}

			base.Query = String.Empty;
		}

		private void searchEntry_FilterChanged (object sender, EventArgs args)
		{
			SearchEntry entry = (SearchEntry)sender;

			int selectedId = entry.ActiveFilterID;
			if (selectedId == 0) {
				entry.EmptyMessage = "Search for files";
			} else {
				string network = entry.GetLabelForFilterID(selectedId);
				entry.EmptyMessage = String.Format("Search '{0}' for files", network);
			}
		}

		private void Core_NetworkAdded (Network network)
		{
			base.AddFilterOption(networkIDs.Count + 1, network.NetworkName);
			networkIDs[networkIDs.Count + 1] = network.NetworkID;
		}
	}
}
