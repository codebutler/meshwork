//
// AboutDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using FileFind.Meshwork;
using System;
using System.Reflection;
using Gtk;
using Glade;

namespace FileFind.Meshwork.GtkClient
{
	public class AboutDialog : GladeDialog
	{
		public AboutDialog (Window parent) : base (parent, "AboutDialog")
		{
			AssemblyDescriptionAttribute desc;
			AssemblyTitleAttribute title;
			AssemblyVersionAttribute version;

			Assembly aAssembly = Assembly.GetExecutingAssembly();

			desc = (AssemblyDescriptionAttribute)AssemblyDescriptionAttribute.GetCustomAttribute(aAssembly, typeof (AssemblyDescriptionAttribute));
			title = (AssemblyTitleAttribute)AssemblyTitleAttribute.GetCustomAttribute(aAssembly, typeof (AssemblyTitleAttribute));
			version = (AssemblyVersionAttribute)AssemblyVersionAttribute.GetCustomAttribute(aAssembly, typeof (AssemblyVersionAttribute));
		 
	 		Gtk.AboutDialog dialog = (Gtk.AboutDialog)Dialog;
			dialog.Name = title.Title;
			//dialog.Version = version.Version;
			dialog.Comments = "Official Meshwork client for Linux";
		}
	}
}
