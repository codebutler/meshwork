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
using FileFind.Meshwork.Logging;

namespace FileFind.Meshwork.GtkClient
{
	public class StatusLogPage : VBox, IPage, ILogger
	{
		TextView m_TextView;
		
		static StatusLogPage s_Instance;
		
		public static StatusLogPage Instance {
			get {
				if (s_Instance == null) {
					s_Instance = new StatusLogPage();
				}
				return s_Instance;
			}
		}

		public event EventHandler UrgencyHintChanged;
		
		public bool UrgencyHint {
			get {
				return false;
			}
		}

		private StatusLogPage ()
		{
			m_TextView = new TextView();
			m_TextView.Editable = false;

			ScrolledWindow swindow = new ScrolledWindow();
			swindow.Add(m_TextView);

			this.PackStart(swindow, true, true, 0);
			swindow.ShowAll();

			var tag = new TextTag("Error");
			tag.Foreground = "darkred";
			m_TextView.Buffer.TagTable.Add(tag);
			
			tag = new TextTag("Fatal");
			tag.Foreground = "darkred";
			m_TextView.Buffer.TagTable.Add(tag);
			
			tag = new TextTag("Warn");
			tag.Foreground = "darkorange";
			m_TextView.Buffer.TagTable.Add(tag);
			
			tag = new TextTag("Info");
			tag.Foreground = "darkgreen";
			m_TextView.Buffer.TagTable.Add(tag);

			tag = new TextTag("Debug");
			tag.Foreground = "darkblue";
			m_TextView.Buffer.TagTable.Add(tag);
			
			m_TextView.Buffer.CreateMark("end", m_TextView.Buffer.EndIter, false);
			
			LoggingService.AddLogger(this);
		}
		
		#region ILogger	implementation
		
		void ILogger.Log (LogLevel level, string message)
		{
			Gtk.Application.Invoke (delegate {
				WriteMessage (level, message);
			});
		}
		
		string ILogger.Name {
			get { return "StatusLogPage"; }
		}

		EnabledLoggingLevel ILogger.EnabledLevel {
			get { return EnabledLoggingLevel.All; } // FIXME: This should be configurable
		}
		
		#endregion
		
		private void WriteMessage (LogLevel level, string message)
		{			
			message = String.Format("{0} [{1}]: {2}\n", level.ToString(), DateTime.Now.ToString("u"), message);
			TextIter endIter = m_TextView.Buffer.EndIter;
			m_TextView.Buffer.InsertWithTagsByName(ref endIter, message, new string[] { level.ToString() });
			var endMark = m_TextView.Buffer.GetMark("end");
			if (endMark != null);
				m_TextView.ScrollToMark(endMark, 0, true, 0, 1);
		}
	}
}
