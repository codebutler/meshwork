//
// BuiltinActionGroup.cs: Built-in toolbar and menu actions
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using Gtk;
using Mono.Unix;

namespace FileFind.Meshwork.GtkClient
{
	public class BuiltinActionGroup : ActionGroup
	{
		private static readonly string string_connect =
			Catalog.GetString ("_Connect to a Friend...");

		private static readonly string string_show_mainwindow =
			Catalog.GetString ("_Show Main Window");

		private static readonly string string_meshwork_menu =
			Catalog.GetString ("_Meshwork");

		/* OSX Only */
		private static readonly string string_network_menu =
			Catalog.GetString ("Network");

		private static readonly string string_post_memo =
			Catalog.GetString ("_Post new Memo...");

		private static readonly string string_join_chat =
			Catalog.GetString ("Create/_Join Chat Room...");

		private static readonly string string_edit_menu =
			Catalog.GetString ("_Edit");

		private static readonly string string_view_menu =
			Catalog.GetString ("_View");

		private static readonly string string_toggle_toolbar =
			Catalog.GetString ("_Toolbar");

		private static readonly string string_toggle_main_users =
			Catalog.GetString ("_User List");

		private static readonly string string_toggle_statusbar =
			Catalog.GetString ("_Status Bar");

		private static readonly string string_help_menu =
			Catalog.GetString ("_Help");

		private static readonly string string_quit = 
			Catalog.GetString ("_Quit");

		private static readonly string string_preferences =
			Catalog.GetString ("_Preferences");

		private static readonly string string_search_again =
			Catalog.GetString ("_Search Again");

		private static readonly string string_remove_search =
			Catalog.GetString ("_Remove");

		private static ActionEntry[] entries = {
			new ActionEntry ("MeshworkMenu", null, string_meshwork_menu,
				null, null, null),

			new ActionEntry ("NetworkMenu", null, string_network_menu,
				null, null, null),

			new ActionEntry ("PostMemo", null, string_post_memo,
				"<control>M", null, null),

			new ActionEntry ("JoinChat", null, string_join_chat,
				"<control>J", null, null),

			new ActionEntry ("Connect", null, string_connect,
				"<control>N", null, null),

			new ActionEntry ("Preferences", Stock.Preferences, string_preferences,
				"<control>P", null, null),

			new ActionEntry ("Quit", null, string_quit, 
				"<control>Q", null, null),

			new ActionEntry ("EditMenu", null, string_edit_menu,
				null, null, null),

			new ActionEntry ("ViewMenu", null, string_view_menu,
				null, null, null),

			new ActionEntry ("HelpMenu", null, string_help_menu,
				null, null, null),

			new ActionEntry ("About", Stock.About),

			new ActionEntry ("SidebarRemoveSearch", null, string_remove_search,
				null, null, null),

			new ActionEntry ("SidebarSearchAgain", null, string_search_again,
				null, null, null)
		};
		
		private static ToggleActionEntry[] toggle_entries = {
			new ToggleActionEntry ("ToggleMainWindow", null, string_show_mainwindow,
				null, null, null, false),

			new ToggleActionEntry ("ToggleMainToolbar", null, string_toggle_toolbar,
				"<control><shift>T", null, null, true),

			new ToggleActionEntry ("ToggleMainUsers", null, string_toggle_main_users,
				"<control><shift>U", null, null, true),

			new ToggleActionEntry ("ToggleMainStatusbar", null, string_toggle_statusbar,
				"<control><shift>S", null, null, true)
		};

		public BuiltinActionGroup () : base ("BuiltinActions")
		{
			Add (entries);
			Add (toggle_entries);

			// Setup Callbacks
			this["Connect"            ].Activated += Connect_Activated;
			this["Preferences"        ].Activated += Preferences_Activated;
			this["ToggleMainWindow"   ].Activated += ToggleMainWindow_Activated;
			this["Quit"               ].Activated += Quit_Activated;
			this["JoinChat"           ].Activated += JoinChat_Activated;
			this["PostMemo"           ].Activated += PostMemo_Activated;
			this["About"              ].Activated += About_Activated;
			this["SidebarSearchAgain" ].Activated += SidebarSearchAgain_Activated;
			this["SidebarRemoveSearch"].Activated += SidebarRemoveSearch_Activated;
			
			this["Connect"].IconName = "list-add";
			this["PostMemo"].IconName = "mail-message-new";
			this["JoinChat"].IconName = "internet-group-chat";
			this["Quit"].IconName = "application-exit";
			this["SidebarRemoveSearch"].IconName = "list-remove";
			this["SidebarSearchAgain"].IconName = "view-refresh";
			
			// Toolbar items need to be important or else no text
			// is displayed. AFAIK, this does not affect menu items
			// at all.
			foreach (Gtk.Action action in base.ListActions()) {
				action.IsImportant = true;

				// XXX: Very strange OSX bug!
				// When an underline is present in the Label, nothing after it is displayed.
				if (Common.OSName == "Darwin") {
					action.Label = action.Label.Replace("_", String.Empty);
				}
			}
		}

		private void Connect_Activated (object sender, EventArgs args)
		{
			ConnectDialog connectWindow = new ConnectDialog();
			connectWindow.Run ();
		}

		private void Preferences_Activated (object sender, EventArgs args)
		{
			PreferencesDialog dialog = new PreferencesDialog ();
			dialog.Run ();
		}

		private void ToggleMainWindow_Activated (object sender, EventArgs args)
		{
			ToggleAction action = (ToggleAction) sender;

			if (action.Active == Gui.MainWindow.IsVisible) 
				return;

			Gui.MainWindow.ToggleVisible ();
		}

		private void Quit_Activated (object sender, EventArgs args)
		{
			Runtime.QuitMeshwork ();
		}

		private void JoinChat_Activated (object sender, EventArgs args)
		{
			JoinChatroomDialog w = new JoinChatroomDialog (Gui.MainWindow.Window);
			if (w.Run() == (int)ResponseType.Ok) {
				Gui.MainWindow.SelectedPage = ChatsPage.Instance;
			}
		}

		private void PostMemo_Activated (object sender, EventArgs args)
		{
			EditMemoDialog w = new EditMemoDialog(Gui.MainWindow.Window);
			if (w.Run() == (int)ResponseType.Ok) {
				MemosPage.Instance.UpdateMemoList();
				Gui.MainWindow.SelectedPage = MemosPage.Instance;
			}
		}

		public void About_Activated (object sender, EventArgs e)
		{
			AboutDialog dialog = new AboutDialog (Gui.MainWindow.Window);
			dialog.Run();
		}

		public void SidebarRemoveSearch_Activated (object sender, EventArgs args)
		{
			FileSearchItem item = (FileSearchItem)Gui.MainWindow.SelectedItem;
			Core.FileSearchManager.RemoveFileSearch(item.Search);
		}

		public void SidebarSearchAgain_Activated (object sender, EventArgs args)
		{
			FileSearchItem item = (FileSearchItem)Gui.MainWindow.SelectedItem;
			item.Search.Repeat();
		}
	}
}
