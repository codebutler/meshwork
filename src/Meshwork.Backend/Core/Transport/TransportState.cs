//
// TransportState.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

namespace Meshwork.Backend.Core.Transport
{
	public enum TransportState
	{
		Waiting,
		Connecting,
		Securing,
		Connected,
		Disconnected
	}
}
