//
// Gui.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using System.Collections.Generic;
using System.Reflection;
using Gtk;
using Meshwork.Client.GtkClient.Windows;
using Meshwork.Client.GtkClient.Pages;
using Meshwork.Backend.Core;
using Meshwork.Client.GtkClient.Platform.Mac;
using Meshwork.Platform.MacOS;

namespace Meshwork.Client.GtkClient
{
	public static class Gui
	{
		static MainWindow mainWindow;

		public static MainWindow MainWindow {
			get {
				return mainWindow;
			}
			set {
				mainWindow = value;
			}
		}

		public static Settings Settings {
			get {
				return (Settings) Runtime.Core.Settings;
			}
		}

		static Dictionary<string, PrivateChatSubpage> privateMessageWindows = new Dictionary<string, PrivateChatSubpage> ();

		public static void StartPrivateChat (Network network, Node node)
		{
 			StartPrivateChat(network, node, true);
		}

		public static PrivateChatSubpage StartPrivateChat (Network network, Node node, bool focus)
		{
			if (node == null)  {
				throw new ArgumentNullException("node");
			}

			if (Runtime.Core.IsLocalNode(node)) {
				Gui.ShowErrorDialog("You cannot send messages to yourself!");
				return null;
			} else if (node.FinishedKeyExchange == true) {
				PrivateChatSubpage page;
				if (privateMessageWindows.ContainsKey(network.NetworkID + node.NodeID) == false) {
					page = new PrivateChatSubpage(network, node);
					privateMessageWindows[network.NetworkID + node.NodeID] = page;
					ChatsPage.Instance.AddPrivateChatSubpage(page);
				} else {
					page = (PrivateChatSubpage)privateMessageWindows[network.NetworkID + node.NodeID];
				}

				if (focus) {
					Gui.MainWindow.SelectedPage = ChatsPage.Instance;
					page.GrabFocus();
				}

				return page;
			} else {
				Gui.ShowErrorDialog("You cannot send messages to untrusted nodes.");
				return null;
			}
		}

		public static PrivateChatSubpage GetPrivateMessageWindow (Node node)
		{
			if (privateMessageWindows.ContainsKey(node.Network.NetworkID + node.NodeID)) {
				return privateMessageWindows[node.Network.NetworkID + node.NodeID];
			} else {
				return null;
			}
		}

		public static void RemovePrivateMessageWindow(Network network, Node node)
		{
			if (privateMessageWindows.ContainsKey(network.NetworkID + node.NodeID)) {
				privateMessageWindows.Remove(network.NetworkID + node.NodeID);
			}
		}

		public static void JoinChatRoom(ChatRoom room)
		{
			JoinChatRoom(room, null);
		}

		public static void JoinChatRoom(ChatRoom room, string password)
		{
			if (room.Properties.ContainsKey("Window") == false) {
				if (room.HasPassword) {
					if (password != null && room.TestPassword(password)) {
						room.Network.JoinChat(room, password);
					} else {
						ChatRoomPasswordDialog dialog =
							new ChatRoomPasswordDialog (Gui.MainWindow.Window, room);
						if (dialog.Run() == (int)ResponseType.Ok) {
							room.Network.JoinChat(room, dialog.Password);
						}
					}
				} else {
					room.Network.JoinChat(room);
				}
			} else {
				(room.Properties ["Window"] as ChatRoomSubpage).GrabFocus();
			}

			Gui.MainWindow.SelectedPage = ChatsPage.Instance;
		}

		public static int ShowMessageDialog (string text, Gtk.Window win, Gtk.MessageType type, Gtk.ButtonsType buttons)
		{
			text = text.Replace (">", "&lt;");
			text = text.Replace ("<", "&gt;");
			text = text.Replace ("&", "&amp;");
			
			if (win == null && Gui.MainWindow != null)
				win = Gui.MainWindow.Window;

			MessageDialog md = new MessageDialog (win, Gtk.DialogFlags.DestroyWithParent, type, buttons, string.Empty);
			md.Title = "Meshwork";
			md.TransientFor = win;
			md.WindowPosition = WindowPosition.CenterOnParent;
			md.Markup = text;
			int result = md.Run ();
			md.Destroy();
			return result;
		}
	
		public static int ShowMessageDialog (string text, Gtk.Window window)
		{
			return ShowMessageDialog (text, window, Gtk.MessageType.Info, ButtonsType.Ok);
		}
		
		public static int ShowMessageDialog (string text)
		{
			return ShowMessageDialog (text, null, Gtk.MessageType.Info, ButtonsType.Ok);
		}

		public static int ShowErrorDialog (string text, Gtk.Window window)
		{
			return ShowMessageDialog (text, window, Gtk.MessageType.Error, Gtk.ButtonsType.Ok);
		}
		
		public static int ShowErrorDialog (string text)
		{
			return ShowErrorDialog (text, null);
		}

		private static int? attentionRequestId = null;

		public static void SetWindowUrgencyHint (Gtk.Window window, bool setting)
		{
			window.UrgencyHint = setting;

			if (Runtime.Core.Platform is OSXPlatform) {
				/* GTK doesn't currenly do anything with the
				 * urgency hint under OSX, so we'll have to
				 * do it ourself for now...
				 */
				var osxApp = new GtkOSXApplication();
				if (setting == true) {
					if (attentionRequestId.HasValue) {
						osxApp.CancelAttentionRequest(attentionRequestId.Value);
					}
					attentionRequestId = osxApp.AttentionRequest(GtkOSXApplicationAttentionType.CriticalRequest);
				} else {
					if (attentionRequestId.HasValue) {
						osxApp.CancelAttentionRequest(attentionRequestId.Value);
						attentionRequestId = null;
					}
				}
			}
		}

		public static void SetError (Gtk.Widget widget, bool setting)
		{
			if (setting == true) {
				widget.ModifyBase (Gtk.StateType.Normal, new Gdk.Color (0xFF, 0x85, 0x85));
				widget.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (0xFF, 0x85, 0x85));
			} else {
				widget.ModifyBase (Gtk.StateType.Normal);
				widget.ModifyBg (Gtk.StateType.Normal);
			}
		}
		
		public static Gdk.Pixbuf LoadIconFromResource (string name, int size)
		{
			try {
				Assembly asm = System.Reflection.Assembly.GetCallingAssembly();
				string resourceName = string.Format("Meshwork.Client.GtkClient.Resources.Images.stock_icons.{0}_{1}.png", name, size.ToString());
				if (asm.GetManifestResourceInfo(resourceName) != null) {
					Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(null, resourceName);
					if (pixbuf != null) {
						return pixbuf;
					}
				}
			} catch (Exception) {
			} 
			return null;
		}
		
		public static Gdk.Pixbuf LoadIconFromTheme (string name, int size) 
		{			
			try {
				Gdk.Pixbuf pixbuf = IconTheme.Default.LoadIcon(name, size, Gtk.IconLookupFlags.UseBuiltin);
				if (pixbuf != null && pixbuf.Width == size && pixbuf.Height == size) {
					return pixbuf;
				}
			} catch (Exception) {
			}
			return null;
		}
		
		public static Gdk.Pixbuf LoadIcon (int size, params string[] names)
		{				
			foreach (string name in names) {
				Gdk.Pixbuf pixbuf = null;
				
				if (Environment.OSVersion.Platform == PlatformID.Unix) {
					pixbuf = LoadIconFromTheme(name, size);
					if (pixbuf == null) {
						LoggingService.LogWarning("Icon not found in theme: {0} {1}", name, size);
						pixbuf = LoadIconFromResource(name, size);
					}
				} else {
					pixbuf = LoadIconFromResource(name, size);
					if (pixbuf == null) {
						pixbuf = LoadIconFromTheme(name, size);
					}
				}
				
				if (pixbuf != null) {
					return pixbuf;
				}
			}
			
			LoggingService.LogWarning("UNABLE TO LOAD ICON {0}, SIZE {1}", string.Join(",",names), size);
			return null;
		}

		public static ScrolledWindow AddScrolledWindow (Widget widget)
		{
			ScrolledWindow swindow = new ScrolledWindow();
			swindow.Add(widget);
			swindow.Show();
			return swindow;
		}
		
		public static AvatarManager AvatarManager {
			get {
				return (AvatarManager) Runtime.Core.AvatarManager;
			}
		}
	}
}
