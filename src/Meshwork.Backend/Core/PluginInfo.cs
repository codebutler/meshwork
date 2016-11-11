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

			Assembly asm = Assembly.LoadFile (fileName);
			foreach (Attribute attr in asm.GetCustomAttributes (true)) {
				if (attr is PluginNameAttribute) {
					this.name = (attr as PluginNameAttribute).Name;

				} else if (attr is PluginDescriptionAttribute) {
					this.description =
						(attr as PluginDescriptionAttribute).Description;

				} else if (attr is PluginAuthorAttribute) {
					this.author = (attr as PluginAuthorAttribute).Author;

				} else if (attr is PluginTypeAttribute) {
					this.type = (attr as PluginTypeAttribute).Type;

				} else if (attr is PluginVersionAttribute) {
					this.version =
						(attr as PluginVersionAttribute).Version;

				} else if (attr is PluginConfDialogTypeAttribute) {
					this.configDialogType = 
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
			Assembly asm = Assembly.LoadFile (fileName);
			return (IPluginConfigDialog)asm.CreateInstance(configDialogType.ToString());
		}

		internal void CreateInstance ()
		{
			if (instance != null) {
				throw new InvalidOperationException ("An instance already exists!");
			}

			// XXX: Load plugins into their own AppDomain so they can be properly unloaded?
			Assembly asm = Assembly.LoadFile (fileName);
			IPlugin plugin = (IPlugin)asm.CreateInstance(type.ToString());
			plugin.Load ();

		}

		internal void DestroyInstance ()
		{
			throw new NotImplementedException ();
		}
	}
}
