//
// AddNetworkDialog.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using Glade;
using Gtk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient.Windows
{
	public class AddNetworkDialog : GladeDialog
	{
		[Widget] Entry networkNameEntry;
		NetworkInfo newNetwork;

		public AddNetworkDialog (Window parentWindow) : base (parentWindow, "AddNetworkDialog")
		{
		}

		public string NetworkName {
			get {
				return networkNameEntry.Text;
			}
		}

		protected override void OnResponded (int responseId)
		{
			if (responseId == (int)ResponseType.Ok) {
				newNetwork = new NetworkInfo ();
				newNetwork.NetworkName = NetworkName;
			}
		}

		public NetworkInfo NetworkInfo {
			get {
				return newNetwork;
			}
		}
	}
}
