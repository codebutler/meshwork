//
// GtkMeshworkOptions.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using Mono.GetOptions;

namespace Meshwork.Client.GtkClient
{
	public class GtkMeshworkOptions : Options
	{
		public GtkMeshworkOptions ()
		{
			base.ParsingMode = OptionsParsingMode.GNU_DoubleDash;
		}

		[Option ("Modify how the main window is initially displayed. May be 'shown', 'iconified', or 'hidden'", "mainwindow-state")]
		public string MainWindowState;

		[Option("Override the default config path", "config-path")]
		public string ConfigPath;
	}
}
