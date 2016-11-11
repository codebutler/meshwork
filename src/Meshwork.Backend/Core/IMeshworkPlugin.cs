//
// IMeshworkPlugin.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace Meshwork.Backend.Core
{
	public interface IPlugin
	{
		void Load ();
		void Unload ();
	}

	public interface IPluginConfigDialog
	{
		void Show (object parentWindow);
		void Hide ();
	}

	public class PluginNameAttribute : Attribute
	{
		string name;

		public PluginNameAttribute (string name)
		{
			this.name = name;
		}

		public string Name {
			get {
				return name;
			}
		}
	}

	public class PluginDescriptionAttribute : Attribute
	{
		string description;

		public PluginDescriptionAttribute (string description)
		{
			this.description = description;
		}

		public string Description {
			get {
				return description;
			}
		}
	}
	
	public class PluginTypeAttribute : Attribute
	{
		Type type;

		public PluginTypeAttribute (Type type)
		{
			this.type = type;
		}

		public Type Type {
			get {
				return type;
			}
		}
	}

	public class PluginConfDialogTypeAttribute : Attribute
	{
		Type type;

		public PluginConfDialogTypeAttribute (Type type)
		{
			this.type = type;
		}

		public Type Type {
			get {
				return type;
			}
		}
	}


	public class PluginAuthorAttribute : Attribute
	{
		string author;

		public PluginAuthorAttribute (string author)
		{
			this.author = author;
		}

		public string Author {
			get {	
				return author;
			}
		}
	}

	public class PluginVersionAttribute : Attribute
	{
		string version;

		public PluginVersionAttribute (string version)
		{
			this.version = version;
		}

		public string Version {
			get {	
				return version;
			}
		}
	}
}

