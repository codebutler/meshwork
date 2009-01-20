//
// IMeshworkOperation.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

namespace FileFind.Meshwork.Transport
{
	public interface IMeshworkOperation
	{
		ITransport Transport {
			get;
		}
	}
}
