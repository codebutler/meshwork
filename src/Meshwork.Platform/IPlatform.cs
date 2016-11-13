//
// IPlatform.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net (http://filefind.net)
//

namespace Meshwork.Platform
{
	public interface IPlatform
	{
		InterfaceAddress[] GetInterfaceAddresses();

	    void SetProcessName(string name);

	    string OSName {
	        get;
	    }
		
	    string UserName {
			get;
		}
		
		string RealName {
			get;
		}

		string VersionInfo {
			get;
		}
	}
}
