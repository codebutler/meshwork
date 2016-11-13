using System;
using System.Reflection;

namespace Meshwork.Backend.Core
{
	public class PluginInfo
	{
		string name;
		string description;
		string author;
		string fileName;
		Type type;
		Type configDialogType;
		string version;
		IPlugin instance = null;

		public PluginInfo (string fileName)
		{
			this.fileName = fileName;

			// XXX: Use cecil here instead of reflection
			// so we dont have to load the assembly!

			var asm = Assembly.LoadFile (fileName);
			foreach (Attribute attr in asm.GetCustomAttributes (true)) {
				if (attr is PluginNameAttribute) {
					name = (attr as PluginNameAttribute).Name;

				} else if (attr is PluginDescriptionAttribute) {
					description =
						(attr as PluginDescriptionAttribute).Description;

				} else if (attr is PluginAuthorAttribute) {
					author = (attr as PluginAuthorAttribute).Author;

				} else if (attr is PluginTypeAttribute) {
					type = (attr as PluginTypeAttribute).Type;

				} else if (attr is PluginVersionAttribute) {
					version =
						(attr as PluginVersionAttribute).Version;

				} else if (attr is PluginConfDialogTypeAttribute) {
					configDialogType = 
						(attr as PluginConfDialogTypeAttribute).Type;
				}
			}
		}

		public string Name {
			get {
				return name;
			}
		}

		public string Description {
			get {
				return description;
			}
		}

		public string Author {
			get {
				return author;
			}
		}

		public string Version {
			get {
				return version;
			}
		}

		public Type Type {	
			get {
				return type;
			}
		}
		
		public string FilePath {
			get {
				return fileName;
			}
		}
	
		public IPluginConfigDialog CreateConfigDialog ()
		{
			var asm = Assembly.LoadFile (fileName);
			return (IPluginConfigDialog)asm.CreateInstance(configDialogType.ToString());
		}

		internal void CreateInstance ()
		{
			if (instance != null) {
				throw new InvalidOperationException ("An instance already exists!");
			}

			// XXX: Load plugins into their own AppDomain so they can be properly unloaded?
			var asm = Assembly.LoadFile (fileName);
			var plugin = (IPlugin)asm.CreateInstance(type.ToString());
			plugin.Load ();

		}

		internal void DestroyInstance ()
		{
			throw new NotImplementedException ();
		}
	}
}
