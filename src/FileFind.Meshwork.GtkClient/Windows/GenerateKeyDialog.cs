//
// GenerateKeyDialog.cs: Key generation status dialog
// 
// Authors:
// 	Eric Butler <eric@extermeboredom.net>
//
// Copyright (C) 2005 FileFind.net
// 

using System;
using System.Security.Cryptography;
using System.Threading;
using Gtk;
using Glade;

namespace FileFind.Meshwork.GtkClient
{
	public class GenerateKeyDialog
	{
		bool keyGenerated = false;
		Dialog dialog;

		[Widget] ProgressBar generateKeyProgress;

		RSAParameters keyParameters;

		public GenerateKeyDialog (Gtk.Window parent)
		{
			Glade.XML winXml = new Glade.XML (null, "FileFind.Meshwork.GtkClient.meshwork.glade", "GenerateKeyDialog", null);
			winXml.Autoconnect (this);
			dialog = (Gtk.Dialog) winXml.GetWidget("GenerateKeyDialog");
			dialog.TransientFor = parent;

			winXml ["dialog-action_area20"].Visible = false;	 
		}

		public int Show ()
		{
			dialog.Show ();
			
			GLib.Timeout.Add (50, new GLib.TimeoutHandler (PulseGenerateKeyProgress));
			
			Thread thread = new Thread (new ThreadStart (GenerateKey));
			thread.Start ();

			int result = 0;

			while (true) {
				result = dialog.Run(); 
				if (result == (int)ResponseType.Ok) 
					break;
			}

			dialog.Destroy ();

			return result;
		}

		public RSAParameters KeyParameters {
			get {
				if (keyGenerated == true)
					return keyParameters;
				else
					throw new InvalidOperationException ();
			}
		}

		private bool PulseGenerateKeyProgress ()
		{
			generateKeyProgress.Pulse ();

			return !keyGenerated;
		}
		
		private void GenerateKey ()
		{
			System.Security.Cryptography.RSACryptoServiceProvider newKey;
			newKey = new System.Security.Cryptography.RSACryptoServiceProvider (2048);

			keyParameters = newKey.ExportParameters (true);
			keyGenerated = true;

			Gtk.Application.Invoke(delegate {
				FinishedGeneratingKey();
			});
		}

		private void FinishedGeneratingKey ()
		{
			dialog.Respond (ResponseType.Ok);
		}
	}
}
