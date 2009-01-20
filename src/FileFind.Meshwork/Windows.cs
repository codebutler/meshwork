//
// Windows.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

/*
 * Created by SharpDevelop.
 * User: Eric
 * Date: 11/2/2006
 * Time: 5:48 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
//using System.Management;
using System.Collections.Generic;
using System.Net;

namespace FileFind.Meshwork
{
	/// <summary>
	/// Description of Windows.
	/// </summary>
	public class WindowsPlatform : IPlatform
	{
		public InterfaceAddress[] GetInterfaceAddresses ()
		{
			List<InterfaceAddress> result = new List<InterfaceAddress> ();

			/*
			string query = "SELECT IPAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = 'TRUE'";
			ManagementObjectSearcher moSearch = new ManagementObjectSearcher (query);
			ManagementObjectCollection moCollection = moSearch.Get ();

			foreach (ManagementObject mo in moCollection) {
				string[] addresses = (string[])mo["IPAddress"];
				string[] descriptions = (string[])mo["Description"];
				
				for (int x = 0; x < addresses.Length; x++) {
					string desc = descriptions[x];
					IPAddress ipaddr = IPAddress.Parse(addresses[x]);
					InterfaceAddress addr = new InterfaceAddress(desc, ipaddr);
					result.Add(addr);
				}
			}
			return result.ToArray();
			 */
			
			IPAddress[] addrs = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
			for (int x = 0; x < addrs.Length; x++) {
				result.Add(new InterfaceAddress(x, "Interface" + x, addrs[x]));
			}
			
			return result.ToArray();
		}
		
		
		public string UserName {
			get {
				return String.Empty;
			}
		}
		
		public string RealName {
			get {
				return String.Empty;
			}
		}

		public string VersionInfo {
			get {
				return "Windows"; // XXX: Get winver
			}
		}
	}
}
