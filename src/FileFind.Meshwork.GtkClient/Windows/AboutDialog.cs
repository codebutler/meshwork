//
// AboutDialog.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Reflection;
using Gtk;

namespace FileFind.Meshwork.GtkClient.Windows
{
	public class AboutDialog : GladeDialog
	{
		public AboutDialog (Window parent) : base(parent, "AboutDialog")
		{
			string title = string.Empty;
			string version = string.Empty;
			
			var assembly = Assembly.GetExecutingAssembly();
			
			var titleAttributes = assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
			if (titleAttributes.Length > 0) {
				AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)titleAttributes[0];
				if (!string.IsNullOrEmpty(titleAttribute.Title)) {
					title = titleAttribute.Title;
				}
			}
			
			var versionAttributes = assembly.GetCustomAttributes(typeof(AssemblyVersionAttribute), false);
			if (versionAttributes.Length > 0) {
				AssemblyVersionAttribute versionAttribute = (AssemblyVersionAttribute)versionAttributes[0];
				if (!string.IsNullOrEmpty(versionAttribute.Version)) {
					version =  versionAttribute.Version;
				}
			}			
			
			Gtk.AboutDialog dialog = (Gtk.AboutDialog)base.Dialog;
			
			if (!string.IsNullOrEmpty(title))
				dialog.ProgramName = title;
			
			if (!string.IsNullOrEmpty(version))
				dialog.Version = version;
		}		
	}
}
