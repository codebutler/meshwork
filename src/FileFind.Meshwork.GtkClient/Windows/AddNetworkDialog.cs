//
// AddNetworkDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using Glade;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
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
