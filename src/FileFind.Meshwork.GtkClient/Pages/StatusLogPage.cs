//
// StatusLogPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005-2007 FileFind.net
// 

using System;
using Gtk;
using FileFind.Meshwork.Transport;

namespace FileFind.Meshwork.GtkClient
{
	public class StatusLogPage : VBox, IPage
	{
		TextView debugTextView;

		public event EventHandler UrgencyHintChanged;

		static StatusLogPage instance;
		public static StatusLogPage Instance {
			get {
				if (instance == null) {
					instance = new StatusLogPage();
				}
				return instance;
			}
		}

		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private StatusLogPage ()
		{
			debugTextView = new TextView();
			debugTextView.Editable = false;

			ScrolledWindow swindow = new ScrolledWindow();
			swindow.Add(debugTextView);

			this.PackStart(swindow, true, true, 0);
			swindow.ShowAll();

			LogManager.Current.NewLogItem +=
				(LogManager.NewLogItemEventHandler)DispatchService.GuiDispatch(
					new LogManager.NewLogItemEventHandler (OnNewLogItem)
				);
			
			foreach (LogEventArgs args in LogManager.Current.GetQueuedLogItems()) {
				OnNewLogItem(args);
			}
		}

		private void OnNewLogItem (LogEventArgs args)
		{
			if (args.Exception == null) {
				WriteStatus (args.Text);
			} else {
				WriteStatus ("{0}: {1}", args.Text, args.Exception.ToString());
			}
		}

		private void WriteStatus (string text, params object[] args)
		{
			if (args != null && args.Length > 0) {
				text = String.Format(text, args);
			}

			Console.WriteLine (text);

			try {
				string str = String.Format("[{0}] {1}", DateTime.Now.ToShortTimeString(), text);
				debugTextView.Buffer.Text += Environment.NewLine + str;
				TextMark EndMark = debugTextView.Buffer.CreateMark ("end", debugTextView.Buffer.EndIter, false);
				debugTextView.ScrollToMark (EndMark, 0.4, true, 0.0, 1.0);
				debugTextView.Buffer.DeleteMark (EndMark);
			} catch (Exception ex) {
				Console.WriteLine ("[!!] IT FUCKED UP: " + ex.ToString());
			}
		}
	}
}
