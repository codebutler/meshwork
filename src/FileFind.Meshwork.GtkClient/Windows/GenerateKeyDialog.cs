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
	public class GenerateKeyDialog : GladeDialog
	{
		bool keyGenerated = false;

		[Widget] ProgressBar generateKeyProgress;

		RSAParameters keyParameters;

		public GenerateKeyDialog (Gtk.Window parent) : base (parent, "GenerateKeyDialog")
		{
			base.Dialog.ActionArea.Visible = false;
		}

		public override int Run()
		{
			
			GLib.Timeout.Add (50, new GLib.TimeoutHandler (PulseGenerateKeyProgress));
			
			Thread thread = new Thread (new ThreadStart (GenerateKey));
			thread.Start ();

			return base.Run();
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
			base.Dialog.Respond(ResponseType.Ok);
		}
	}
}
