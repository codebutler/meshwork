using System.Collections.Generic;
using Debug;
using Gtk;
using Meshwork.Backend.Core;
using Meshwork.Client.GtkClient;
using System;

[assembly: PluginName("Debug Console")]
[assembly: PluginAuthor("Eric Butler <eric@codebutler.com>")]
[assembly: PluginVersion("0.0.0.1")]
[assembly: PluginType(typeof(DebugPlugin))]

namespace Debug
{
	public class DebugPlugin : IPlugin
	{
		DebugPluginActionGroup debug_actions;
		UIManager ui_manager;
		uint merge_id;
		DebugWindow debug_window;
		List<MessageInfo> messages = new List<MessageInfo>();
		bool trafficLogEnabled;
		Core core;

		public void Load (Core core)
		{
			this.core = core;
			core.Started += (EventHandler)DispatchService.GuiDispatch(new EventHandler(Core_Started));
	    }

		private void Core_Started(object sender, EventArgs args)
		{
			debug_actions = new DebugPluginActionGroup(this);
			debug_window = new DebugWindow(this);

			ui_manager = Runtime.UIManager;
			ui_manager.InsertActionGroup(debug_actions, 0);

			merge_id = ui_manager.AddUiFromResource("DebugPluginMenus.xml");
		}

		public void Unload ()
		{
			ui_manager.RemoveUi(merge_id);

			core.MessageReceived -= AddMessage;
			core.MessageSent -= AddMessage;
			core = null;
		}

		public bool EnableTrafficLog {
			get {
				return trafficLogEnabled;
			}
			set {
				if (value) {
					if (!trafficLogEnabled) {
						core.MessageReceived += AddMessage;
						core.MessageSent += AddMessage;
					}
				} else {
					if (trafficLogEnabled) {
						core.MessageReceived -= AddMessage;
						core.MessageSent -= AddMessage;
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

		internal Core Core {
			get {
				return core;
			}
		}

		private void AddMessage (MessageInfo messageInfo)
		{
			messages.Add(messageInfo);
			debug_window.AddMessage(messageInfo);
		}
	}
}
