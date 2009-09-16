using System;
using System.Collections.Generic;
using Gtk;
using FileFind.Meshwork;
using FileFind.Meshwork.GtkClient;

[assembly: PluginName("Debug Console")]
[assembly: PluginAuthor("Eric Butler <eric@extremeboredom.net>")]
[assembly: PluginVersion("0.0.0.1")]
[assembly: PluginType(typeof(FileFind.Meshwork.DebugPlugin.DebugPlugin))]

namespace FileFind.Meshwork.DebugPlugin
{
	public class DebugPlugin : IPlugin
	{
		DebugPluginActionGroup debug_actions;
		UIManager ui_manager;
		uint merge_id;
		DebugWindow debug_window;
		List<MessageInfo> messages = new List<MessageInfo>();
		bool trafficLogEnabled;

		public DebugPlugin ()
		{
			debug_actions = new DebugPluginActionGroup(this);
			debug_window = new DebugWindow(this);
		}

		public void Load ()
		{
			ui_manager = Runtime.UIManager;
			ui_manager.InsertActionGroup(debug_actions, 0);
			merge_id = ui_manager.AddUiFromResource("DebugPluginMenus.xml");
		}

		public void Unload ()
		{
			ui_manager.RemoveUi(merge_id);

			Core.MessageReceived -= AddMessage;
			Core.MessageSent -= AddMessage;
		}

		public bool EnableTrafficLog {
			get {
				return trafficLogEnabled;
			}
			set {
				if (value) {
					if (!trafficLogEnabled) {
						Core.MessageReceived += AddMessage;
						Core.MessageSent += AddMessage;
					}
				} else {
					if (trafficLogEnabled) {
						Core.MessageReceived -= AddMessage;
						Core.MessageSent -= AddMessage;
					}
				}
				trafficLogEnabled = value;
			}
		}

		public MessageInfo[] Messages {
			get {
				return messages.ToArray();
			}
		}

		internal DebugWindow DebugWindow {
			get {
				return debug_window;
			}
		}

		private void AddMessage (MessageInfo messageInfo)
		{
			messages.Add(messageInfo);
			debug_window.AddMessage(messageInfo);
		}
	}
}
