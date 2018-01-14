//
// IPlatform.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
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
