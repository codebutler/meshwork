//
// INodeConnection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System.Net;

using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;

namespace FileFind.Meshwork {
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
