using System;
using System.Text;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Serialization;
using FileFind;
using FileFind.Meshwork;
using System.IO;

namespace FileFind.Meshwork.DaemonClient
{
 	public class SettingsCreator
	{
		public SettingsCreator (string fileName)
		{
			// Create settings file
			Settings settings = new Settings ();
			settings.FileName = fileName;
			
			Console.Write("NickName = ");
			settings.NickName = Console.ReadLine();

			Console.Write("\nReal Name = ");
			settings.RealName = Console.ReadLine();
		
			Console.Write("\nGenerating keypair....");

			System.Security.Cryptography.RSACryptoServiceProvider newKey;
			newKey = new System.Security.Cryptography.RSACryptoServiceProvider (2048);
			settings.SetKey(newKey.ToXmlString(true));
			
			Console.Write("Done!\n\nNow you need to define a network.\n\n");

			NetworkInfo networkInfo = new NetworkInfo();

			Console.Write("Network Name = ");
			networkInfo.NetworkName = Console.ReadLine();

			settings.Networks.Add(networkInfo);

			Console.WriteLine("All done, saving settings now!");

			if (fileName.IndexOf(Path.DirectorySeparatorChar) > -1) {
				settings.DataPath = fileName.Substring(0, fileName.LastIndexOf(Path.DirectorySeparatorChar));
			} else {
				settings.DataPath = Environment.CurrentDirectory;
			}

			settings.SaveSettings();

			Console.WriteLine("WARNING: The first person who connects to me will become the admin!");
		}
	}
}
