//
// INodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

namespace Meshwork.Backend.Core {
	public interface INodeConnection {

		Node NodeLocal {
			get;
			set;
		}

		Node NodeRemote {
			get;
			set;
		}

		ConnectionState ConnectionState {
			get;
		}
	}
}
