//
// Gui.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Gtk;
using FileFind.Meshwork.GtkClient.Windows;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace FileFind.Meshwork.GtkClient
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
				return (Settings)Core.Settings;
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

			if (Core.IsLocalNode(node)) {
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
						room.Network.JoinChat (room.Name, password);
					} else {
						ChatRoomPasswordDialog dialog =
							new ChatRoomPasswordDialog (Gui.MainWindow.Window, room);
						if (dialog.Run() == (int)ResponseType.Ok) {
							room.Network.JoinChat (room.Name, dialog.Password);
						}
					}
				} else {
					room.Network.JoinChat (room.Name);
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

			MessageDialog md = new MessageDialog (win, Gtk.DialogFlags.DestroyWithParent, type, buttons, String.Empty);
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
			return ShowMessageDialog (text, Gui.MainWindow.Window, Gtk.MessageType.Info, ButtonsType.Ok);
		}

		public static int ShowErrorDialog (string text, Gtk.Window window)
		{
			return ShowMessageDialog (text, window, Gtk.MessageType.Error, Gtk.ButtonsType.Ok);
		}
		
		public static int ShowErrorDialog (string text)
		{
			return ShowErrorDialog (text, Gui.MainWindow.Window);
		}
		
		static Imendio.MacIntegration.AttentionRequest lastDockAttentionRequest = null;
		public static void SetWindowUrgencyHint (Gtk.Window window, bool setting)
		{
			window.UrgencyHint = setting;

			if (Common.OSName == "Darwin") {
				/* GTK doesn't currenly do anything with the
				 * urgency hint under OSX, so we'll have to
				 * do it ourself for now...
				 */
				
				Imendio.MacIntegration.Dock dock = Imendio.MacIntegration.Dock.Default;
				if (setting == true) {
					if (lastDockAttentionRequest != null) {
						dock.AttentionCancel(lastDockAttentionRequest);
					}
					lastDockAttentionRequest = dock.AttentionRequest(Imendio.MacIntegration.AttentionType.Critical);
				} else {
					if (lastDockAttentionRequest != null) {
						dock.AttentionCancel(lastDockAttentionRequest);
						lastDockAttentionRequest = null;
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
		
		private delegate Gdk.Pixbuf IconLoadDelegate(string name, int size);
		public static Gdk.Pixbuf LoadIcon (int size, params string[] names)
		{				
			Assembly asm = System.Reflection.Assembly.GetCallingAssembly();
			
			IconLoadDelegate loadFromGtkTheme = delegate(string thisName, int thisSize) {
				// Try to load from gtk theme...
				try {
					Gdk.Pixbuf pixbuf1 = IconTheme.Default.LoadIcon (thisName, thisSize, Gtk.IconLookupFlags.UseBuiltin);
					if (pixbuf1 != null) {
						return pixbuf1;
					}
				} catch (Exception) {
				}
				return null;
			};
				
			IconLoadDelegate loadFromResource = delegate(string thisName, int thisSize) {
				// If that fails, try from resource...
				try {
					string resourceName = String.Format("FileFind.Meshwork.GtkClient.{0}_{1}.png", thisName, thisSize.ToString());
					if (asm.GetManifestResourceInfo (resourceName) != null) {
						Gdk.Pixbuf pixbuf2 = new Gdk.Pixbuf (null, resourceName);
						if (pixbuf2 != null) {
							return pixbuf2;
						}
					}
				} catch (Exception) {
				} 
				return null;
			};
		
			foreach (string name in names) {
				Gdk.Pixbuf pixbuf = null;
				
				if (Environment.OSVersion.Platform == PlatformID.Unix) {
					pixbuf = loadFromGtkTheme(name, size);
					if (pixbuf == null) {
						pixbuf = loadFromResource(name, size);
					}
				} else {
					pixbuf = loadFromResource(name, size);
					if (pixbuf == null) {
						pixbuf = loadFromGtkTheme(name, size);
					}
				}
				
				if (pixbuf != null) {
					return pixbuf;
				}
			}
			
			LoggingService.LogWarning("UNABLE TO LOAD ICON {0}, SIZE {1}", String.Join(",",names), size);
			return null;
		}

		public static ScrolledWindow AddScrolledWindow (Widget widget)
		{
			ScrolledWindow swindow = new ScrolledWindow();
			swindow.Add(widget);
			swindow.Show();
			return swindow;
		}
	}
}
