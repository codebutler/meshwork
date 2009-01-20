//
// LogManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;

namespace FileFind.Meshwork
{
	public class LogEventArgs 
	{
		DateTime timestamp;
		 string text;
		 Exception ex;
		 
		 internal LogEventArgs (string text, Exception ex) 
		 {
		 	this.timestamp = DateTime.Now;
		 	this.text = text;
		 	this.ex = ex;
		 }
		 
		 public DateTime Timestamp {
		 	get {
		 		return timestamp;
		 	}
		 }
		 
		 public string Text {
		 	get {
		 		return text;
		 	}
		 }
		 
		 public Exception Exception {
		 	get {
		 		return ex;
		 	}
		 }
	}
	
	public class LogManager
	{
		private static LogManager currentLogManager;

		public static LogManager Current {
			get {
				if (currentLogManager == null)
					currentLogManager = new LogManager ();
				return currentLogManager;
			}
		}
		
		public delegate void NewLogItemEventHandler (LogEventArgs args);

		public event NewLogItemEventHandler NewLogItem;
		
		bool logEnabled = true;
		Queue<LogEventArgs> queuedLogItems = new Queue<LogEventArgs>();
		
		public LogEventArgs[] GetQueuedLogItems()
		{
			List<LogEventArgs> result = new List<LogEventArgs>();
			lock (queuedLogItems) {
				while (queuedLogItems.Count > 0) {
					result.Add(queuedLogItems.Dequeue());
				}
			}
			return result.ToArray();
		}
		
		public bool LogEnabled {
			get {
				return logEnabled;
			}
		}

		public void WriteToLog (object o)
		{
			WriteToLog (o.ToString());
		}
		
		
		public void WriteToLog (string text)
		{
			WriteToLog(text, null, null);
		}
		
		public void WriteToLog (string text, params object[] args)
		{
			WriteToLog(text, null, args);
		}
		
		public void WriteToLog (string text, Exception ex)
		{
			WriteToLog(text, ex, null);
		}
		
		public void WriteToLog (string text, Exception ex, params object[] args)
		{
			if (LogEnabled == true) {
				if (args != null) {
					text = String.Format(text, args);
				}
				LogEventArgs eventArgs = new LogEventArgs(text, ex);
				if (NewLogItem != null) {
					NewLogItem (eventArgs);
				} else {
					lock (queuedLogItems) {
						queuedLogItems.Enqueue(eventArgs);
					}
					Console.Error.WriteLine("WARNING: no event handler for new log item!\n\n" + text);
				}
			}
		}
	}
}
