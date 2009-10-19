//
// PreferencesDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Gtk;
using Glade;
using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Destination;

namespace FileFind.Meshwork.GtkClient
{	
	public class PreferencesDialog : GladeDialog
	{
		// General Tab
		[Widget] Entry  nicknameEntry;
		[Widget] Entry  nameEntry;
		[Widget] Entry  emailEntry;
		[Widget] Button avatarButton;
		[Widget] Image  avatarImage;
		[Widget] Label  nodeIdLabel;
		[Widget] Table  downloadPathsTable;

		// Friends & Networks Tab
		[Widget] TreeView networksTreeView;
		
		// Connection Tab
		[Widget] Label tcpPortLabel;
		[Widget] Label firewallStatusLabel;
		[Widget] Label natStatusLabel;
		[Widget] Label internetIPLabel;
		[Widget] Label supportsIPv6Label;
		[Widget] Image firewallImage;
		[Widget] Image computerImage;
		[Widget] Image internetConnectionImage;
		[Widget] Button redetectConnectionButton;

		// File Sharing Tab
		[Widget] TreeView          sharedFoldersList;
		[Widget] FileChooserButton downloadsChooser;
		[Widget] FileChooserButton completedDownloadsChooser;

		// Plugins Tab
		[Widget] TreeView pluginsTreeView;
		
		// Advanced Tab
		[Widget] Notebook advancedNotebook;
		[Widget] TreeView advancedList;

		// Advanced Tab -> Auto-Connect
		[Widget] TreeView   autoConnectList;
		[Widget] SpinButton autoConnectCountSpinButton;

		// Advanced Tab -> Connection
		[Widget] SpinButton  nodePortSpinButton;
		[Widget] CheckButton nodePortOpenCheckButton;
		[Widget] Button      testTcpPortButton;

		[Widget] ComboBox    ipv6LocalInterfaceComboBox;
		
		[Widget] Table       natOptionsTable;
		[Widget] CheckButton detectIPCheckButton;
		[Widget] Entry       externalIPv4AddressEntry;
		[Widget] Button      detectInternetIPButton;
		[Widget] Entry       stunServerEntry;

		// Advanced Tab -> Apperance
		[Widget] CheckButton startInTrayCheckButton;

		// Advanced Tab -> File Transfer
		[Widget] CheckButton limitDownSpeedCheckButton;
		[Widget] CheckButton limitUpSpeedCheckButton;
		[Widget] SpinButton limitDownSpeedSpinButton;
		[Widget] SpinButton limitUpSpeedSpinButton;
		
		ListStore networksListStore;
		Gtk.ListStore sharedFoldersListStore;
		Gtk.ListStore advancedListStore;
		Gtk.TreeStore autoConnectTreeStore;
		Gtk.ListStore pluginsListStore;

		Gtk.Dialog dialog;
		Gdk.Pixbuf folderImage;

		Settings settings;

		string nodeid = null;
		RSACryptoServiceProvider provider;
		
		private static TargetEntry [] target_table = new TargetEntry [] {
			new TargetEntry ("STRING", 0, (uint) 0 ),
			new TargetEntry ("text/plain", 0, (uint) 0),
			new TargetEntry ("application/x-rootwindow-drop", 0, (uint) 0)
		};
		
		public PreferencesDialog () : base (null, "PreferencesDialog")
		{
			dialog = base.Dialog;
			dialog.Shown += delegate {
				if (settings.FirstRun) {
					on_redetectConnectionButton_clicked(redetectConnectionButton, EventArgs.Empty);
				}
			};
			
			settings = Gui.Settings;

			/* Configure gui */

			firewallImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.firewall-small.png");
			internetConnectionImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.network1.png");
			folderImage = Gui.LoadIcon (24, "folder");
		
			sharedFoldersListStore = new Gtk.ListStore(typeof(string));
			sharedFoldersList.Model = sharedFoldersListStore;
			var imageCell = new CellRendererPixbuf();
			var textCell = new CellRendererText();
			var column = new TreeViewColumn();
			column.PackStart(imageCell, false);
			column.PackStart(textCell, true);
			column.SetCellDataFunc(imageCell, showFolderIcon);
			column.SetCellDataFunc(textCell, showFolderText);
			sharedFoldersList.AppendColumn(column);
			sharedFoldersList.RulesHint = true;

			Gtk.Drag.DestSet (sharedFoldersList, Gtk.DestDefaults.All, new Gtk.TargetEntry [] { new Gtk.TargetEntry ("text/uri-list", 0, 0) }, Gdk.DragAction.Copy);
			sharedFoldersList.DragDataReceived += OnSharedFoldersListDragDataReceived;
			
			advancedListStore = new Gtk.ListStore (typeof(string), typeof(int));
			advancedList.Model = advancedListStore;
			advancedList.AppendColumn("Text", new CellRendererText(), "text", 0);
			
			advancedNotebook.ShowTabs = false;
	
			for (int x = 0; x < advancedNotebook.NPages; x++) {
				Widget widget = advancedNotebook.GetNthPage(x);
				advancedListStore.AppendValues (advancedNotebook.GetTabLabelText(widget), x);
			}

			TreeIter iter;
			advancedListStore.GetIterFirst(out iter);
			advancedList.Selection.SelectIter(iter);
			
			if (Gui.MainWindow != null) {
				dialog.TransientFor = Gui.MainWindow.Window;
			} else {
				// First run!
			}

			Gtk.Drag.DestSet (avatarButton, DestDefaults.All, target_table, Gdk.DragAction.Copy | Gdk.DragAction.Move);


			provider = new RSACryptoServiceProvider();
			provider.ImportParameters(settings.EncryptionParameters);
			nodeid = Common.MD5(provider.ToXmlString(false));

			/**** Load options ****/
	
			// General Tab		
			nicknameEntry.Text = settings.NickName;
			nameEntry.Text = settings.RealName;
			nodeIdLabel.Text = nodeid;
			emailEntry.Text = settings.Email;

			string avatarDirectory = Path.Combine (Settings.ConfigurationDirectory, "avatars");
			string myAvatarFile = Path.Combine (avatarDirectory, String.Format ("{0}.png", nodeid));

			if (File.Exists (myAvatarFile)) {
				avatarImage.Pixbuf = new Gdk.Pixbuf (myAvatarFile);
			} else {
				avatarImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.avatar-generic-large.png");
				avatarImage.Sensitive = false;
			}
	
			// Networks tab
			networksListStore = new ListStore (typeof (NetworkInfo));

			foreach (NetworkInfo networkInfo in settings.Networks) {
				networksListStore.AppendValues(networkInfo.Clone());
			}

			networksTreeView.AppendColumn ("Network Name", new CellRendererText(), new TreeCellDataFunc (NetworkNameFunc));
			networksTreeView.Model = networksListStore;
			
			// File Sharing Tab

			foreach (string dir in settings.SharedDirectories) {
				sharedFoldersListStore.AppendValues(new object[] {dir});
			}
			downloadsChooser.SetCurrentFolder (settings.IncompleteDownloadDir);
			completedDownloadsChooser.SetCurrentFolder (settings.CompletedDownloadDir);
			
			// Connection Tab
			
			tcpPortLabel.Text = settings.TcpListenPort.ToString();

			firewallStatusLabel.Text = String.Empty;

			if (CheckForNat()) {
				natStatusLabel.Markup = "You <b>are</b> behind a NAT router.";
				// XXX: Include UPnP Info!
			} else {
				natStatusLabel.Markup = "You <b>are not</b> behind a NAT router.";
				natOptionsTable.Sensitive = false;
			}

			bool foundIPv6Internal = false;
			bool foundIPv6External = false;
			foreach (IDestination destination in Core.DestinationManager.Destinations) {
				if (destination is IPv6Destination) {
					if (((IPv6Destination)destination).IsExternal) {
						foundIPv6External = true;
					} else {
						foundIPv6Internal = true;
					}
				} else if (destination is IPv4Destination && destination.IsExternal) {
					internetIPLabel.Text = ((IPDestination)destination).IPAddress.ToString();
				}
			}
			if (foundIPv6External) {
				supportsIPv6Label.Text = "Yes";
			} else if (foundIPv6Internal) {
				supportsIPv6Label.Text = "LAN Only";
			} else {
				supportsIPv6Label.Text = "No";
			}

			// Plugins Tab
			
			pluginsListStore = new ListStore (typeof(PluginInfo));
			pluginsTreeView.AppendColumn ("Plugin Info", new CellRendererText(), new TreeCellDataFunc (PluginInfoFunc));
			pluginsTreeView.Model = pluginsListStore;

			foreach (string fileName in settings.Plugins) {
				try {
					PluginInfo info = new PluginInfo (fileName);
					pluginsListStore.AppendValues (info);
				} catch (Exception ex) {
					LoggingService.LogError(ex);
				}
			}

			// Advanced -> Appearance

			startInTrayCheckButton.Active = settings.StartInTray;
			
			// Advanced -> Auto-connect Tab		
			autoConnectTreeStore = new Gtk.TreeStore (typeof(object));
			autoConnectList.Model = autoConnectTreeStore;

			CellRendererToggle autoConnectToggleCell = new CellRendererToggle();
			autoConnectToggleCell.Toggled += OnAutoConnectItemToggled;

			CellRendererText autoConnectTextCell = new CellRendererText ();

			column = new TreeViewColumn ();
			column.PackStart (autoConnectToggleCell, false);
			column.SetCellDataFunc (autoConnectToggleCell, new TreeCellDataFunc(ShowAutoConnectToggle));
			column.PackStart (autoConnectTextCell, true);
			column.SetCellDataFunc (autoConnectTextCell, new TreeCellDataFunc(ShowAutoConnectName));
			autoConnectList.AppendColumn (column);
			autoConnectList.AppendColumn ("IP", new CellRendererText (), new Gtk.TreeCellDataFunc (ShowAutoConnectIP));
			PopulateAutoConnectList ();
			autoConnectCountSpinButton.Value = settings.AutoConnectCount;
			
			// Advanced -> Connection
			nodePortSpinButton.Value       = settings.TcpListenPort;
			nodePortOpenCheckButton.Active = settings.TcpListenPortOpen;
			detectIPCheckButton.Active     = settings.DetectInternetIPOnStart;
			externalIPv4AddressEntry.Text  = internetIPLabel.Text;
			stunServerEntry.Text           = settings.StunServer;

			ipv6LocalInterfaceComboBox.Model = new ListStore(typeof(string), typeof(int));
			((ListStore)ipv6LocalInterfaceComboBox.Model).AppendValues("Disabled", -1);
			var interfaces = new Dictionary<string, int>();
			foreach (InterfaceAddress addr in Core.OS.GetInterfaceAddresses()) {
				if (addr.Address.AddressFamily == AddressFamily.InterNetworkV6 && (!IPAddress.IsLoopback(addr.Address))) {
					if (!interfaces.ContainsKey(addr.Name))
						interfaces[addr.Name] = addr.InterfaceIndex;
				}
			}
			foreach (string name in interfaces.Keys) {
					((ListStore)ipv6LocalInterfaceComboBox.Model).AppendValues(name, interfaces[name]);
			}

			if (ipv6LocalInterfaceComboBox.Model.GetIterFirst(out iter)) {
				do {
					int index = (int)ipv6LocalInterfaceComboBox.Model.GetValue(iter, 1);
					if (index == settings.IPv6LinkLocalInterfaceIndex) {
						ipv6LocalInterfaceComboBox.SetActiveIter(iter);
						break;
					}
				} while (ipv6LocalInterfaceComboBox.Model.IterNext(ref iter));
			}

			UpdateFirewallLabel();

			// Advanced -> File Transfer
			limitDownSpeedCheckButton.Active = settings.EnableGlobalDownloadSpeedLimit;
			limitDownSpeedSpinButton.Value = settings.GlobalDownloadSpeedLimit;

			limitUpSpeedCheckButton.Active = settings.EnableGlobalUploadSpeedLimit;
			limitUpSpeedSpinButton.Value = settings.GlobalUploadSpeedLimit;

			// I cant seem to make anything default with just the glade file.
			nicknameEntry.GrabFocus();
		}

		private void PopulateAutoConnectList ()
		{
			autoConnectTreeStore.Clear ();
			foreach (object[] row in networksListStore) {
				NetworkInfo networkInfo = (NetworkInfo)row[0];
				TreeIter networkIter = autoConnectTreeStore.AppendValues (networkInfo);
				foreach (TrustedNodeInfo nodeInfo in networkInfo.TrustedNodes.Values) {
					autoConnectTreeStore.AppendValues (networkIter, nodeInfo);
				}
			}
		}

		private void NetworkNameFunc (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			NetworkInfo networkInfo = (NetworkInfo)model.GetValue (iter, 0);
			(cell as CellRendererText).Text = String.Format ("{0} ({1} friends)", networkInfo.NetworkName, networkInfo.TrustedNodes.Count);
		}

		private void addNetworkButton_Clicked (object o, EventArgs args)
		{
			AddNetworkDialog addDialog = new AddNetworkDialog(dialog);
			if (addDialog.Run() == (int)ResponseType.Ok) {
				foreach (object[] row in networksListStore) {
					NetworkInfo networkInfo = (NetworkInfo)row[0];
					if (networkInfo.NetworkName == addDialog.NetworkInfo.NetworkName) {
						Gui.ShowErrorDialog("A network with that name has already been added.");
						return;
					}
				}

				networksListStore.AppendValues(addDialog.NetworkInfo);
				PopulateAutoConnectList();
			}
		}

		private void removeNetworkButton_Clicked (object sender, EventArgs args)
		{
			TreeIter iter;
			if (networksTreeView.Selection.GetSelected (out iter) == true) {
				if (Gui.ShowMessageDialog ("Are you sure you want to delete this network? All friends will be lost.", 
				                           Dialog, Gtk.MessageType.Question, ButtonsType.YesNo) == (int)ResponseType.Yes)
			 	{
					networksListStore.Remove (ref iter);
					PopulateAutoConnectList();
				}
			}

		}
	
		private void networksTreeView_RowActivated (object sender, RowActivatedArgs args)
		{
			TreeIter iter;
			if (networksListStore.GetIter (out iter, args.Path) == true) {
				NetworkInfo networkInfo = (NetworkInfo) networksListStore.GetValue (iter, 0);
				EditNetworkDialog editNetworkDialog = new EditNetworkDialog (dialog, networkInfo);
				editNetworkDialog.Run ();
				PopulateAutoConnectList ();
			}
		}

		private void OnSharedFoldersListDragDataReceived (object o, DragDataReceivedArgs args)
		{
			bool success = false;
			string allPaths = System.Text.Encoding.UTF8.GetString (args.SelectionData.Data);

			string[] paths = allPaths.Split ('\n');

			foreach (string path in paths) {
				string cleanPath = path.Trim ();
				if (cleanPath != "") {
					LoggingService.LogDebug(cleanPath);

					Uri uri = new Uri (cleanPath);
	
					if (uri.IsFile) {
						if (System.IO.Directory.Exists (uri.LocalPath) == true) {
							sharedFoldersListStore.AppendValues(new object[] { uri.LocalPath });
							success = true;
						}
					}
				}
			}
			Gtk.Drag.Finish (args.Context, success, false, args.Time);
		}
		
		private void OnAutoConnectItemToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			if (autoConnectTreeStore.GetIter (out iter, new TreePath (args.Path))) {
				TrustedNodeInfo node = (TrustedNodeInfo) autoConnectTreeStore.GetValue (iter, 0);
				node.AllowAutoConnect = !node.AllowAutoConnect;
				autoConnectList.QueueDraw ();
			}
		}

		private void ShowAutoConnectToggle (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererToggle toggleCell = (CellRendererToggle) cell;

			if (((TreeStore)model).IterDepth (iter) > 0) {
				TrustedNodeInfo node = (TrustedNodeInfo) model.GetValue (iter, 0);

				toggleCell.Active = node.AllowAutoConnect;
				toggleCell.Activatable = true;
				toggleCell.Visible = true;
			} else {
				toggleCell.Visible = false;
			}
		}
	
		private void ShowAutoConnectName (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textCell = (CellRendererText) cell;

			if (((TreeStore)model).IterDepth (iter) == 0) {
				NetworkInfo networkInfo = (NetworkInfo)model.GetValue (iter, 0);
				textCell.Markup = String.Format ("<b>{0}</b>", GLib.Markup.EscapeText(networkInfo.NetworkName));
			} else {
				TrustedNodeInfo node = (TrustedNodeInfo) model.GetValue (iter, 0);
				textCell.Markup = String.Format ("{0} <span size=\"small\" foreground=\"#666666\">({1})</span>",
				                                 node.Identifier,
				                                 node.NodeID);
			}
		}

		private void ShowAutoConnectIP (TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			CellRendererText textCell = (CellRendererText) cell;

			if (((TreeStore)model).IterDepth (iter) > 0) {
				TrustedNodeInfo node = (TrustedNodeInfo) model.GetValue (iter, 0);
				IDestination destination = node.FirstConnectableDestination;
				if (destination == null) {
					textCell.Text = "<unknown>";
				} else {
					textCell.Text = destination.ToString();
				}
			} else {
				textCell.Text = String.Empty;
			}
		}
		
		private void on_copyKeyButton_clicked(object o, EventArgs args)
		{
			string publicKey = new PublicKey(nicknameEntry.Text, provider.ToXmlString(false)).ToArmoredString();
			Gtk.Clipboard.GetForDisplay(Gdk.Display.Default, Gdk.Atom.Intern("CLIPBOARD", true)).Text = publicKey;
			Gui.ShowMessageDialog("The clipboard has been set.", dialog);
		}
		
		private void on_saveKeyButton_clicked(object o, EventArgs args)
		{
			FileSelector selector = new FileSelector("Save Public Key...", FileChooserAction.Save);
			selector.TransientFor = dialog;
			selector.CurrentName = nicknameEntry.Text + ".mpk";
			if (selector.Run() == (int)ResponseType.Ok) {
				string publicKey = new PublicKey(nicknameEntry.Text, provider.ToXmlString(false)).ToArmoredString();
				FileFind.Common.WriteToFile(selector.Filename,publicKey);
			}
			selector.Destroy();
		}
		
		private void on_addSharedFolderButton_clicked(object o, EventArgs args)
		{
			FolderDialog selectFolderDialog = new FolderDialog ("Select Folder");
			selectFolderDialog.TransientFor = dialog;
			if (selectFolderDialog.Run() == (int)ResponseType.Ok) {
				sharedFoldersListStore.AppendValues(new object[] { selectFolderDialog.CurrentFolder });
			}
			selectFolderDialog.Destroy();
		}
		
		private void on_removeSharedFolderButton_clicked(object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			sharedFoldersList.Selection.GetSelected(out model, out iter);
			if (sharedFoldersListStore.IterIsValid(iter))
				sharedFoldersListStore.Remove(ref iter);
		}
		
		private void detectInternetIPButton_Clicked (object sender, EventArgs args)
		{
			base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
			Button button = (Button)sender;
			button.Sensitive = false;
			ThreadPool.QueueUserWorkItem(delegate {			                                              
				string publicIP = DetectPublicIP();
				Application.Invoke(delegate {
					externalIPv4AddressEntry.Text = publicIP;
					button.Sensitive = true;
					base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.LeftPtr);
				});
			});
		}

		private void testTcpPortButton_clicked (object sender, EventArgs args)
		{
			base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
			Button button = (Button)sender;
			button.Sensitive = false;
			ThreadPool.QueueUserWorkItem(delegate {
				bool portOpen = TestTCPPort();
				Application.Invoke(delegate {
					nodePortOpenCheckButton.Active = portOpen;
					button.Sensitive = true;
					base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.LeftPtr);
				});
			});				                   
		}

		private void nodePortOpenCheckButton_Toggled (object sender, EventArgs args)
		{
			UpdateFirewallLabel();
		}

		private void on_redetectConnectionButton_clicked(object o, EventArgs args)
		{
			// This button acts as if all other buttons were clicked.
			base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
			Button button = (Button)o;
			button.Sensitive = false;
			ThreadPool.QueueUserWorkItem(delegate {
				string publicIP = DetectPublicIP();
				bool portOpen = TestTCPPort();
				Application.Invoke(delegate {
					nodePortOpenCheckButton.Active = portOpen;
					externalIPv4AddressEntry.Text = publicIP;
					button.Sensitive = true;
					base.Dialog.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.LeftPtr);
				});				
			});
		}
		
		private string DetectPublicIP ()
		{
			try {
				return FileFind.Stun.StunClient.GetExternalAddress().ToString();
			} catch (Exception) {
				return "<Error>";
			}
		}
		
		private bool TestTCPPort ()
		{
			try {
				WebResponse resp = HttpWebRequest.Create("http://filefind.net/test_port.php?port=" + nodePortSpinButton.Value.ToString()).GetResponse();
				using (resp) {
					using (StreamReader reader = new StreamReader(resp.GetResponseStream())) {
						string response = reader.ReadToEnd();
						if (response == "1")
							return true;
					}
				}
			} catch (Exception) { /* ignore */ }
			return false;
		}
		
		private void showFolderText(TreeViewColumn treeColumn, CellRenderer cellRenderer, TreeModel treeModel, TreeIter treeIter)
		{
			string path = treeModel.GetValue (treeIter, 0).ToString ();
			if (System.IO.Directory.Exists (path) == false)
				(cellRenderer as Gtk.CellRendererText).Markup = "<span foreground=\"red\">" + path + " (Not Found)</span>";
			else
				(cellRenderer as Gtk.CellRendererText).Markup = path;
		}

		private void showFolderIcon(TreeViewColumn treeColumn, CellRenderer cellRenderer, TreeModel treeModel, TreeIter treeIter)
		{
			CellRendererPixbuf renderer = (CellRendererPixbuf)cellRenderer;
			renderer.Pixbuf = folderImage;
		}
		
		private void on_advancedList_button_release_event(object o, ButtonReleaseEventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			if (advancedList.Selection.GetSelected(out model, out iter)) {
				int index = (int)advancedListStore.GetValue(iter, 1);
				advancedNotebook.Page = index;
			}
		}
		
		private void on_okButton_clicked (object o, EventArgs args)
		{
			dialog.Respond((int)Gtk.ResponseType.None);

			ArrayList badOptions = new ArrayList();
			
			if (nicknameEntry.Text == String.Empty)
				badOptions.Add("Nickname");

			if (Directory.Exists(completedDownloadsChooser.CurrentFolder) == false)
				badOptions.Add("Completed dowmnloads directory");

			if (Directory.Exists(downloadsChooser.CurrentFolder) == false)
				badOptions.Add("Incomplete dowmnloads directory");


 			if (Common.IsValidIP(externalIPv4AddressEntry.Text) == false) {
				badOptions.Add("External IP Address");
			}

			if (networksListStore.IterNChildren() == 0) {
				badOptions.Add("Please define at least one network");
			}

			if (badOptions.Count > 0) {
				BadOptionsDialog badOptionsWindow = new BadOptionsDialog(dialog, badOptions);
				badOptionsWindow.Run();
				return;
			}
			
			settings.NickName = nicknameEntry.Text;
			settings.RealName = nameEntry.Text;
			settings.Email = emailEntry.Text;

			settings.Networks.Clear();
			foreach (object[] row in networksListStore) {
				settings.Networks.Add((NetworkInfo)row[0]);
			}

			settings.SharedDirectories.Clear();

			TreeIter iter;
			sharedFoldersListStore.GetIterFirst(out iter);
			if (sharedFoldersListStore.IterIsValid(iter)) {
				foreach (object[] currentRow in sharedFoldersListStore) {
					settings.SharedDirectories.Add((string)currentRow[0]);
				}
			}

			settings.CompletedDownloadDir = completedDownloadsChooser.CurrentFolder;
			settings.IncompleteDownloadDir = downloadsChooser.CurrentFolder;
			
			settings.SavedDestinationInfos.Clear();

			TCPIPv4Destination destination = new TCPIPv4Destination(IPAddress.Parse(externalIPv4AddressEntry.Text), (uint)nodePortSpinButton.Value, nodePortOpenCheckButton.Active);
			DestinationInfo externalIPInfo = destination.CreateDestinationInfo();
			externalIPInfo.Local = true;
			settings.SavedDestinationInfos.Add(externalIPInfo);
			
			settings.TcpListenPortOpen = nodePortOpenCheckButton.Active;
			settings.TcpListenPort = (int)nodePortSpinButton.Value;

			if (ipv6LocalInterfaceComboBox.GetActiveIter(out iter)) {
				int index = (int)ipv6LocalInterfaceComboBox.Model.GetValue(iter, 1);
				settings.IPv6LinkLocalInterfaceIndex = index;
			} else {
				settings.IPv6LinkLocalInterfaceIndex = -1;
			}

			if (stunServerEntry.Text != String.Empty) {
				settings.StunServer = stunServerEntry.Text;
			} else {
				settings.StunServer = Settings.DefaultStunServer;
			}

			settings.StartInTray = startInTrayCheckButton.Active;
			settings.AutoConnectCount = autoConnectCountSpinButton.ValueAsInt;

			string avatarDirectory = Path.Combine(Settings.ConfigurationDirectory, "avatars");

			if (Directory.Exists(avatarDirectory) == false)
				Directory.CreateDirectory(avatarDirectory);

			string myAvatarFile = Path.Combine(avatarDirectory, String.Format("{0}.png", nodeid));

			//if (avatarImage.Pixbuf != null)
			if (avatarImage.Sensitive == true)
				avatarImage.Pixbuf.Save(myAvatarFile, "png");
			else
				if (File.Exists(myAvatarFile))
				File.Delete(myAvatarFile);
			
			settings.Plugins.Clear();
			foreach (object[] row in pluginsListStore) {
				PluginInfo info = (PluginInfo)row[0];
				settings.Plugins.Add(info.FilePath);
			}

			
			settings.FirstRun = false;
			settings.SaveSettings();
			
			if (Core.AvatarManager != null)
				Core.AvatarManager.UpdateMyAvatar();

			// Advanced -> File Transfer
			settings.EnableGlobalDownloadSpeedLimit = limitDownSpeedCheckButton.Active;
			settings.GlobalDownloadSpeedLimit = Convert.ToInt32(limitDownSpeedSpinButton.Value);

			settings.EnableGlobalUploadSpeedLimit = limitUpSpeedCheckButton.Active;
			settings.GlobalUploadSpeedLimit = Convert.ToInt32(limitUpSpeedSpinButton.Value);

			// Save and go!
			Core.Settings = Gui.Settings;
			dialog.Respond ((int)Gtk.ResponseType.Ok);
		}

		private void UpdateFirewallLabel()
		{
			if (nodePortOpenCheckButton.Active == true) {
				firewallStatusLabel.Markup = "<span color=\"darkgreen\">No Firewall was found to be blocking Meshwork!</span>";
			} else {
				firewallStatusLabel.Markup = "Incoming connections to this port appear to be blocked or are not properly forwarded, check your router/firewall.\n<span size=\"small\">Meshwork <b>will</b> operate properly with the port closed.</span>";
			}
		}

		private void on_emailEntry_changed (object o, EventArgs args)
		{
			//Gui.SetError ((Gtk.Widget)o, ! FileFind.Common.IsValidEmail (((Gtk.Entry)o).Text));
		}

		private void externalIPv4AddressEntry_Changed (object o, EventArgs args)
		{
			internetIPLabel.Text = externalIPv4AddressEntry.Text;
		}

		private void on_avatarButton_clicked (object o, EventArgs args)
		{
			FileSelector selector = new FileSelector ("Select Image");
			selector.Show ();
			int result = selector.Run ();
			if (result == (int)Gtk.ResponseType.Ok) {
				try {
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (selector.Filename);
					
					if (pixbuf.Width > 80 | pixbuf.Height > 80)
						pixbuf = pixbuf.ScaleSimple (80, 80, Gdk.InterpType.Hyper);

					avatarImage.Pixbuf = pixbuf;
					avatarImage.Sensitive = true;
				} catch (Exception ex) {
					selector.Hide ();
					Gui.ShowMessageDialog (ex.Message);
					return;
				}
			}
			selector.Hide ();
		
		}

		[GLib.ConnectBefore]
		private void on_avatarButton_button_press_event (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3) {
				Gtk.Menu menu = new Gtk.Menu ();
				ImageMenuItem clearItem = new ImageMenuItem (Gtk.Stock.Clear, null);
				clearItem.Activated += clearItem_Activated;	
				menu.Append (clearItem);
				menu.ShowAll ();
				menu.Popup ();
			}
		}

		private void clearItem_Activated (object o, EventArgs args)
		{
			//avatarImage.Pixbuf = null;
			avatarImage.Pixbuf = new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.avatar-generic-large.png");
			avatarImage.Sensitive = false;
		}

		private void on_avatarButton_drag_data_received (object sender, DragDataReceivedArgs args)
		{
			if (args.SelectionData.Length >=0 && args.SelectionData.Format == 8) {
				//Console.WriteLine ("Received {0} in label", args.SelectionData.Text);
				try {
					string fileName = new Uri (args.SelectionData.Text.Trim ()).LocalPath;
					
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf (fileName);
						
					if (pixbuf.Width > 80 | pixbuf.Height > 80)
						pixbuf = pixbuf.ScaleSimple (80, 80, Gdk.InterpType.Hyper);

					avatarImage.Pixbuf = pixbuf;
					avatarImage.Sensitive = true;

				} catch (Exception ex) {
					Gui.ShowMessageDialog (ex.Message);
				}

			}
	
			Gtk.Drag.Finish (args.Context, false, false, args.Time);
		}

		private void nodePortOpenCheckButton_toggled (object o, EventArgs args)
		{
			UpdateFirewallLabel ();
		}

		private void nodePortSpinButton_changed (object o, EventArgs args)
		{
			UpdateFirewallLabel ();
			tcpPortLabel.Text = nodePortSpinButton.Value.ToString();
		}

		private void addPluginButton_Clicked (object sender, EventArgs args)
		{
			FileSelector selector = new FileSelector ("Select plugin to load");
			selector.Show ();
			int result = selector.Run ();
			if (result == (int)Gtk.ResponseType.Ok) {
				try {
					PluginInfo info = new PluginInfo (selector.Filename);
					pluginsListStore.AppendValues (info);
				} catch (Exception ex) {
					LoggingService.LogError(ex);
				}
			}
			selector.Destroy();
		}

		private void removePluginButton_Clicked (object sender, EventArgs args)
		{

		}
		
		private void PluginInfoFunc (TreeViewColumn column, CellRenderer cell,
		                             TreeModel model, TreeIter iter)
		{
			PluginInfo info = (PluginInfo)model.GetValue (iter, 0);
			CellRendererText textCell = (CellRendererText)cell;
			textCell.Markup = String.Format ("<b>{0}</b> v{1}\n{2}",
			                                 GLib.Markup.EscapeText (info.Name),
							 GLib.Markup.EscapeText (info.Version),
			                                 GLib.Markup.EscapeText (info.Description));
		}
		
		private void pluginsTreeView_RowActivated (object sender, RowActivatedArgs args)
		{
			TreeIter iter;
			if (pluginsListStore.GetIter (out iter, args.Path) == true) {
				PluginInfo pluginInfo = (PluginInfo) pluginsListStore.GetValue (iter, 0);
				IPluginConfigDialog configDialog = pluginInfo.CreateConfigDialog ();
				configDialog.Show (dialog);
			}
		}

		private bool CheckForNat() 
		{
			foreach (IDestination destination in Core.DestinationManager.Destinations) {
				if (destination is IPv4Destination && !destination.IsExternal) {
					return true;
				}
			}
			return false;
		}

		private void limitDownSpeedCheckButton_Toggled (object sender, EventArgs args)
		{
			limitDownSpeedSpinButton.Sensitive = limitDownSpeedCheckButton.Active;
		}

		private void limitUpSpeedCheckButton_Toggled (object sender, EventArgs args)
		{
			limitUpSpeedSpinButton.Sensitive = limitUpSpeedCheckButton.Active;
		}
	}
}
