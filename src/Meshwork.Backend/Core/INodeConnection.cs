//
// INodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
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
