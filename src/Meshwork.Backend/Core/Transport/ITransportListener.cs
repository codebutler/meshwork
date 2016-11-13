//
// ITransportListener.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace Meshwork.Backend.Core.Transport
{
	public interface ITransportListener
	{
		void StartListening ();
		void StopListening ();
	}

	public class FailedTransportListener
	{
		public ITransportListener Listener;
		public Exception          Error;

		public FailedTransportListener (ITransportListener listener, Exception error)
		{
			Listener = listener;
			Error    = error;
		}
	}
}
