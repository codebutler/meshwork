using System;
using System.IO;
using System.Security.Cryptography;
using Meshwork.Backend.Core;

namespace Meshwork.Client.Console
{
 	public class SettingsCreator
	{
		public SettingsCreator (string fileName)
		{
			// Create settings file
			var settings = new Settings ();
			settings.FileName = fileName;

			System.Console.Write("NickName = ");
			settings.NickName = System.Console.ReadLine();

			System.Console.Write("\nReal Name = ");
			settings.RealName = System.Console.ReadLine();

			System.Console.Write("\nGenerating keypair....");

		    var newKey = new RSACryptoServiceProvider (2048);
		    settings.PrivateKey = newKey.ToXmlString(true);

			System.Console.Write("Done!\n\nNow you need to define a network.\n\n");

			var networkInfo = new NetworkInfo();

			System.Console.Write("Network Name = ");
			networkInfo.NetworkName = System.Console.ReadLine();

			settings.Networks.Add(networkInfo);

			System.Console.WriteLine("All done, saving settings now!");

			if (fileName.IndexOf(Path.DirectorySeparatorChar) > -1) {
				settings.DataPath = fileName.Substring(0, fileName.LastIndexOf(Path.DirectorySeparatorChar));
			} else {
				settings.DataPath = Environment.CurrentDirectory;
			}

			settings.SaveSettings();

			System.Console.WriteLine("WARNING: The first person who connects to me will become the admin!");
		}
	}
}
