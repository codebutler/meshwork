//
// ConnectionState.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

namespace Meshwork.Backend.Core
{
	public enum ConnectionState
	{
		Connecting,
		Authenticating,
		Remote,
		Ready,
		Disconnected
	}
}
