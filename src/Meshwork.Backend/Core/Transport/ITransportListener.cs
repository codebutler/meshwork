//
// ITransportListener.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
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
