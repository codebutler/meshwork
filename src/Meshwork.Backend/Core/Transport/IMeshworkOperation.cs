//
// IMeshworkOperation.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Core.Transport
{
	public interface IMeshworkOperation
	{
		ITransport Transport {
			get;
		}
	}
}
