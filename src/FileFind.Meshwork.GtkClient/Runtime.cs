//
// Runtime.cs:
//
// Authors:
//   Eric Butler <eric@filefind.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// Software), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED AS IS, WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Gtk;
using System;
using System.Collections;
using System.Threading;
using System.IO;

using Mono.Unix;

namespace FileFind.Meshwork.GtkClient
{
	public class Runtime
	{
		static SplashWindow splashWindow;
		static BuiltinActionGroup builtin_actions;
		static UIManager ui_manager;
		static GtkMeshworkOptions options;
		static TrayIcon trayIcon;

		private static Settings tmpSettings;

		public static void Main (string[] args)
		{
			/* Initialize our catalog */
			//   Catalog.Init (Defines.Name, Defines.LocaleDir);

			/* Process our args */
			options = new GtkMeshworkOptions ();
			options.ProcessArgs (args);
			
			Common.SetProcessName("meshwork-gtk");

			/* Initialize the GTK application */
			Gtk.Application.Init();
			
			/* If we crash, attempt to log the error */
			GLib.ExceptionManager.UnhandledException += UnhandledExceptionHandler;
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(AppDomain_CurrentDomain_UnhandledException);;
			
			//XXX: Implement Gunique code here!

			splashWindow = new SplashWindow ();
			splashWindow.Show ();

			/* Load settings */
			if (options.ConfigPath != null) {
				LoggingService.LogDebug("Using config dir: " + options.ConfigPath);
				Settings.OverrideConfigPath(options.ConfigPath);
			}
			tmpSettings = Settings.ReadSettings();

			// First run, create initial settings.
			if (tmpSettings == null) {
				tmpSettings = new Settings();
				tmpSettings.NickName = Core.OS.UserName;
				tmpSettings.RealName = Core.OS.RealName;
				tmpSettings.IncompleteDownloadDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				tmpSettings.CompletedDownloadDir  = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				tmpSettings.FirstRun = true;
			}
		
			/* Load Icons */
			Gtk.Window.DefaultIconList = new Gdk.Pixbuf[] {
				new Gdk.Pixbuf(null, "FileFind.Meshwork.GtkClient.tray_icon.png")
			};
			
			// XXX: This is the deprecated API:
			IconSet chatIconSet = new IconSet (new Gdk.Pixbuf (null, "FileFind.Meshwork.GtkClient.internet-group-chat_24.png"));
			IconFactory factory = new IconFactory ();
			factory.Add ("internet-group-chat", chatIconSet);
			factory.AddDefault ();

			if (Environment.OSVersion.Platform != PlatformID.Unix) {
				
				// Windows specific. Override stock icons to use embeded files.
				Gtk.IconTheme.AddBuiltinIcon("gtk-add", 24, Gui.LoadIcon(24, "add"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-add", 16, Gui.LoadIcon(16, "add"));
				
				Gtk.IconTheme.AddBuiltinIcon("gtk-remove", 24, Gui.LoadIcon(24, "remove"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-remove", 16, Gui.LoadIcon(16, "remove"));
				
				Gtk.IconTheme.AddBuiltinIcon("gtk-preferences", 24, Gui.LoadIcon(24, "gtk-preferences"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-preferences", 16, Gui.LoadIcon(16, "gtk-preferences"));
				
				Gtk.IconTheme.AddBuiltinIcon("gtk-new", 24, Gui.LoadIcon(24, "gtk-new"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-new", 16, Gui.LoadIcon(16, "gtk-new"));
				
				Gtk.IconTheme.AddBuiltinIcon("gtk-find", 24, Gui.LoadIcon(24, "find"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-find", 16, Gui.LoadIcon(16, "find"));
				
				Gtk.IconTheme.AddBuiltinIcon("gtk-home", 24, Gui.LoadIcon(24, "home"));
				Gtk.IconTheme.AddBuiltinIcon("gtk-home", 16, Gui.LoadIcon(16, "home"));
			}

			/* Set up UI actions */
			builtin_actions = new BuiltinActionGroup ();
			ui_manager = new UIManager ();
			ui_manager.InsertActionGroup (builtin_actions, 0);
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.MainWindow.xml");
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.TrayPopupMenu.xml");
			ui_manager.AddUiFromResource ("FileFind.Meshwork.GtkClient.SearchPopupMenu.xml");

			/* Create the Tray Icon */
			trayIcon = new TrayIcon();

			/* Add default error handler */
			AppDomain.CurrentDomain.UnhandledException += delegate (object sender, UnhandledExceptionEventArgs e) {
				// XXX: Can we pop up an error dialog too?
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Black;
				Console.WriteLine(e.ExceptionObject.ToString()); Console.ResetColor();
			};

			/* Start the event loop */
			GLib.Idle.Add (new GLib.IdleHandler (FinishLoading));
			Gtk.Application.Run ();
		}

		public static UIManager UIManager {
			get {
				return ui_manager;
			}
		}

		public static BuiltinActionGroup BuiltinActions {
			get {
				return builtin_actions;
			}
		}

		private static bool FinishLoading ()
		{
			Core.Started += (EventHandler)DispatchService.GuiDispatch(new EventHandler(Core_Started));

			if (tmpSettings.FirstRun) {
				LoggingService.LogDebug("First run");

				// Generate key
				if (tmpSettings.EncryptionParameters.Modulus == null) {
					GenerateKeyDialog keyDialog = new GenerateKeyDialog (null);
					keyDialog.Run();
					tmpSettings.EncryptionParameters = keyDialog.KeyParameters;
				}

				/* Init the core */
				Core.Init(tmpSettings);

				splashWindow.Close();

				PreferencesDialog preferences = new PreferencesDialog ();
				if (preferences.Run () != (int)ResponseType.Ok) {
					// Abort !!
					Gtk.Application.Quit ();
					Environment.Exit(1);
					return false;
				}
				Core.Settings = Settings.ReadSettings();
			} else {
				/* Init the core */
				Core.Init(tmpSettings);
			}

			tmpSettings = null;

			Core.AvatarManager = (IAvatarManager) new AvatarManager();
			
			/* Load the gui */
			Gui.MainWindow = new MainWindow ();

			Gdk.Screen screen = Gdk.Screen.Default;
		
			if (Common.OSName == "Linux") {
				Gdk.Colormap colormap = screen.RgbaColormap;
				if (colormap != null) {
					Widget.DefaultColormap = colormap;
					Gtk.Widget.PushColormap(colormap);
				}
			}
			
			splashWindow.Close();

			if ((!Gui.Settings.StartInTray && options.MainWindowState != "hidden") ||
			    (Gui.Settings.StartInTray && (options.MainWindowState == "shown" | options.MainWindowState == "iconified"))) {
				Gui.MainWindow.Show();
				if (options.MainWindowState == "iconified") {
					Gui.MainWindow.Iconify();
				}
			}

			Thread thread = new Thread(delegate () {
				Core.Start();
			});
			thread.Start();

			return false;
		}

		public static bool QuitMeshwork()
		{
			try {
				int result = Gui.ShowMessageDialog ("Are you sure you want to quit Meshwork?", Gui.MainWindow.Window, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo);
				if (result == (int)ResponseType.Yes) {
					Gui.Settings.SaveSettings ();
					Core.Stop();
					Gtk.Application.Quit();
					Environment.Exit(0);
					return true;
				} else {
					return false;
				}
			} catch (Exception ex) {
				LoggingService.LogError(ex);
				throw ex;
			}
		}
		
		private static void AppDomain_CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs args)
		{
			Console.Error.WriteLine("UNHANDLED EXCEPTION!! " + args.ExceptionObject.ToString());
			string crashFileName = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), String.Format("meshwork-crash-{0}.log", DateTime.Now.ToFileTime()));
			string crashLog = args.ExceptionObject.ToString();
			FileFind.Common.WriteToFile(crashFileName, crashLog);
		}
		
		private static void UnhandledExceptionHandler (GLib.UnhandledExceptionArgs args) 
		{
			Console.Error.WriteLine("UNHANDLED EXCEPTION!! " + args.ExceptionObject.ToString());
			string crashFileName = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), String.Format("meshwork-crash-{0}.log", DateTime.Now.ToFileTime()));
			string crashLog = args.ExceptionObject.ToString();
			FileFind.Common.WriteToFile(crashFileName, crashLog);
			
			args.ExitApplication = true;
			
			Gui.ShowErrorDialog("Meshwork has encountered an unhandled error and must be closed.\n\nAn error report has been created on your desktop, please file a bug.\n\n" + args.ExceptionObject.ToString());
		}

		private static void Core_Started (object sender, EventArgs args)
		{
			if (Core.FailedTransportListeners.Length > 0) {
				StartupProblemsDialog dialog = new StartupProblemsDialog();
				dialog.Run();
			}

		}
	}
}
