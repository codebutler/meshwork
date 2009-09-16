using System;
using Gtk;
using Mono.Unix;

namespace FileFind.Meshwork.DebugPlugin
{
	public class DebugPluginActionGroup : ActionGroup
	{
		DebugPlugin plugin;

		private static readonly string string_debug_window =
			Catalog.GetString ("_Debug Window");

		private static ActionEntry[] entries = {
			new ActionEntry("ShowDebugWindow", null, string_debug_window,
				null, null, null)
		};

		public DebugPluginActionGroup (DebugPlugin plugin) : base ("DebugPluginActions")
		{
			Add(entries);

			this.plugin = plugin;

			this["ShowDebugWindow"].Activated += ShowDebugWindow_Activated;
		}
		
		private void ShowDebugWindow_Activated (object sender, EventArgs args)
		{
			plugin.DebugWindow.Show();
		}
	}
}
