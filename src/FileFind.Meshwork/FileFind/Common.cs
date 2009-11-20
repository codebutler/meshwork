//
// Common.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using System.Diagnostics;

using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using FileFind;
using FileFind.Meshwork;
using FileFind.Meshwork.Collections;
using FileFind.Meshwork.Exceptions;
using FileFind.Meshwork.Destination;

namespace FileFind
{
	public static class Common
	{
		public static bool IsValidEmail (string inputEmail)
		{
			string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
			  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
			  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
			Regex re = new Regex (strRegex);
			if (re.IsMatch (inputEmail))
				return (true);
			else
				return (false);
		}	


		public static ulong GetUnixTimestamp()
		{
     			TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
     			return (ulong)t.TotalSeconds;
		}

		public static DateTime ParseUnixTimestamp (ulong time)
		{
			DateTime epoch = new DateTime(1970, 1, 1);
			return epoch.AddSeconds(time);
		}
		
		public static bool IsNumeric(string N) {
			try {
				Int64.Parse(N);
			} catch (Exception) {
				return false;
			}
			return true;
		}
		
		public static bool SupportsIPv6 {
			get {
				// XXX: This doesnt work? return Socket.OSSupportsIPv6;
				try {
					Socket tmp = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
					tmp.Close();
					return true;
				}
				catch { 
					return false;
				}
			}
		}
		
		public static bool HasExternalIPv6 {
			get {
				foreach (IDestination destination in Core.DestinationManager.Destinations) {
					if (destination is IPv6Destination && ((IPv6Destination)destination).IsExternal) {
						return true;
					}
				}
				return false;
			}
		}

		public static string BytesToString(byte[] b) {
			string tmpstr = null;
			if (!(b == null)) {
				for (int x = 0; x <= b.Length - 1; x++) {
					tmpstr += b[x].ToString("X2");
				}
			}
			return tmpstr;
		}
		
		public static byte[] StringToBytes(string str)
		{
			if ((str.Length % 2) != 0) {
				throw new ArgumentException("Str has bad length");
			}
			
			byte[] result = new byte[str.Length / 2];
			
			for (int x = 0; x < str.Length; x += 2) {
				result[x/2] = byte.Parse(str.Substring(x, 2), NumberStyles.HexNumber);
			}
			
			return result;
		}

		public static string MD5(byte[] bytesIn) {
			byte[] bytesOut = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(bytesIn);
			string stringOut = BitConverter.ToString(bytesOut).Replace("-", string.Empty);
			return stringOut.ToLower ();
		}

		public static string MD5(string stringIn) {
			byte[] bytesIn = System.Text.Encoding.UTF8.GetBytes(stringIn);
			byte[] bytesOut = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(bytesIn);
			string stringOut = BitConverter.ToString(bytesOut).Replace("-", string.Empty);
			return stringOut.ToLower ();
		}

		public static string SHA(byte[] bytesIn) {
			SHA1 SHAObj = SHA1.Create();
			byte[] bytesOut = SHAObj.ComputeHash(bytesIn);
			string strOut = BitConverter.ToString(bytesOut).Replace("-","");
			return strOut.ToLower ();
		}

		public static byte[] SHA512 (string stringIn) 
		{
			SHA512 shaManaged = new SHA512Managed ();
			return shaManaged.ComputeHash (System.Text.Encoding.ASCII.GetBytes (stringIn));
		}
		 

		public static string SHA512Str (string stringIn)
		{
			return BitConverter.ToString(SHA512(stringIn)).Replace("-","");
		}

		public static string AddLineBreaks(string strIn)
		{
			string strOut = "";
			while (strIn != "") {
				if (strIn.Length >= 65) {
					strOut += strIn.Substring(0, 65) + Environment.NewLine;
					strIn = strIn.Substring(65);
				} else {
					strOut += strIn;
					strIn = "";
				}
			}
			return strOut;
		}

		public static bool IsInternalIP (IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork) {
				byte[] bytes = address.GetAddressBytes();
				return (bytes[0] == 10) || (bytes[0] == 192 && bytes[1] == 168) ||
				       (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31)) || 
				       (bytes[0] == 169 && bytes[1] == 254);
			} else if (address.AddressFamily == AddressFamily.InterNetworkV6) {
				return address.IsIPv6LinkLocal;
			} else {
				throw new ArgumentException("address must be IPv4 or IPv6");
			}
		}

		public static string FormatBytes (decimal bytes)
		{
			if (bytes >= 1099511627776)
				return Math.Round((bytes / 1024 / 1024 / 1024 / 1024),2).ToString() + " TB";
			else if (bytes >= 1073741824)
				return Math.Round((bytes / 1024 / 1024 / 1024),2).ToString() + " GB";
			else if (bytes >= 1048576)
				return Math.Round((bytes / 1024 / 1024),2).ToString() + " MB";
			else if (bytes >= 1024)
				return Math.Round((bytes / 1024),2).ToString() + " KB";
			else if (bytes < 1024)
				return bytes.ToString() + " Bytes";
			else
				return "0 Bytes";
		}

		public static string FormatNumber (long number)
		{
			System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo( "en-US", false ).NumberFormat;
			nfi.NumberDecimalDigits = 0;
			return number.ToString("N",nfi);
		}

		public static bool WildcardMatch (string text, string pattern)
		{
			return (Regex.Match(text, ToRegexPattern(pattern)).Success == true);
		}

		/* From the mono class library source */
		/* /mcs/class/System.Web/System.Web.Configuration/HandlerItem.cs */
		private static string ToRegexPattern (string dosPattern)
		{
                        string result = dosPattern.Replace (".", "\\.");
                        result = result.Replace ("*", ".*");
                        result = result.Replace ('?', '.');
                        return result;
                }

		public static bool IsValidIP (string ip)
		{
			/* XXX: Perhaps use these beautiful regexes I found...
			// IPv4
			if (Regex.IsMatch (address, @"(\d{1,3}\.){3}\d{1,3}") == true) {
				return true;
			// IPv6
			} else if (Regex.IsMatch (address, @"^((([0-9A-Fa-f]{1,4}:){7}[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){6}:[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){5}:([0-9A-Fa-f]{1,4}:)?[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){4}:([0-9A-Fa-f]{1,4}:){0,2}[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){3}:([0-9A-Fa-f]{1,4}:){0,3}[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){2}:([0-9A-Fa-f]{1,4}:){0,4}[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){6}((\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b)\.){3}(\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b))|(([0-9A-Fa-f]{1,4}:){0,5}:((\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b)\.){3}(\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b))|(::([0-9A-Fa-f]{1,4}:){0,5}((\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b)\.){3}(\b((25[0-5])|(1\d{2})|(2[0-4]\d)|(\d{1,2}))\b))|([0-9A-Fa-f]{1,4}::([0-9A-Fa-f]{1,4}:){0,5}[0-9A-Fa-f]{1,4})|(::([0-9A-Fa-f]{1,4}:){0,6}[0-9A-Fa-f]{1,4})|(([0-9A-Fa-f]{1,4}:){1,7}:))$") == true) {
				return true;
			} else {
				return false;
			}
			*/

			try {
				IPAddress.Parse(ip);
				return true;
			} catch (Exception) {
				return false;
			}
		}

	/*	private static Regex GetRegex (string verb) {
                    //    EnsureCache ();
                      //  if (regexCache.ContainsKey (verb))
                      //          return (Regex) regexCache [verb];

                        StringBuilder result = new StringBuilder ("\\A");
                        string [] expressions = verb.Split (',');
                        int end = expressions.Length;
                        for (int i = 0; i < end; i++) {
                                string regex = ToRegexPattern (expressions [i]);
                                if (i + 1 < end) {
                                        result.AppendFormat ("{0}\\z|\\A", regex);
                                } else {
                                        result.AppendFormat ("({0})\\z", regex);
                                }
                        }
                        Regex r = new Regex (result.ToString ());
                     //  regexCache [verb] = r;
                        return r;
                }*/

		public static bool ParseSizeString(string str, out ulong num, out string unitName)
		{
			str = Regex.Replace(str, @"\s+", "");
			
			unitName = null;
			num = 0;

			Dictionary<string,string> suffixes = new Dictionary<string,string>();
			suffixes.Add("b", "bit");
			suffixes.Add("B", "byte");
			suffixes.Add("Kb", "kilobit");
			suffixes.Add("KB", "kilobyte");
			suffixes.Add("Mb", "megabit");
			suffixes.Add("MB", "megabyte");
			suffixes.Add("Gb", "gigabit");
			suffixes.Add("GB", "gigabyte");

			// These are a bit ambiguous but commonly used...
			suffixes.Add("mb", "megabyte");
			suffixes.Add("m", "megabyte");
			suffixes.Add("M", "megabyte");
			suffixes.Add("k", "kilobyte");
			suffixes.Add("K", "kilobyte");
			suffixes.Add("kb", "kilobyte");
			suffixes.Add("gb", "gigabyte");
			suffixes.Add("G", "gigabyte");
			suffixes.Add("g", "gigabyte");
		
			// Check that the string has numbers followed by letters,
			// for example "10MB".
			Match match = Regex.Match(str, @"^(\d+)([A-Za-z]+)$");
			if (match.Success) {
				if (UInt64.TryParse(match.Groups[1].Captures[0].Value.Trim(), out num)) {
					unitName = match.Groups[2].Captures[0].Value.Trim();
				}
			} else {
				// If all numeric, default to megabytes
				match = Regex.Match(str, @"^(\d+)$");
				if (match.Success) {
					if (UInt64.TryParse(match.Groups[1].Captures[0].Value, out num)) {
						unitName = "mb";
					}
				}
				if (unitName == null) {
					unitName = "shait";
				}
			}

			if (unitName != null && suffixes.ContainsKey(unitName)) {
				unitName = suffixes[unitName];
				return true;
			} else {
				unitName = null;
				return false;
			}
		}

		public static bool ValidateSizeStr (string str)
		{
			ulong num;
			string unitName;
			return (ParseSizeString(str, out num, out unitName));
		}

		public static ulong SizeStringToBytes (string str)
		{
			ulong num;
			string unitName;
			if (ParseSizeString(str, out num, out unitName)) {
				switch (unitName) {
					case "byte":
						return num;
					case "kilobyte":
						return num * 1024;
					case "megabyte":
						return num * 1048576;
					case "gigabyte":
						return num * 1073741824;
					default:
						return 0;
				}
			} else {
				return 0;
			}
		}
		
		static string uname = null;
		public static string OSName {
			get {
				if (Environment.OSVersion.Platform == PlatformID.Unix) {
					if (uname == null) {
						ProcessStartInfo info = new ProcessStartInfo("uname");
						info.UseShellExecute = false;
						info.RedirectStandardOutput = true;
						Process process = Process.Start(info);
						process.WaitForExit();
						uname = process.StandardOutput.ReadToEnd().Trim();

					}
					return uname;
				} else {
					return "Windows";
				}
			}
		}
		
		[DllImport ("libc")] // Linux
		private static extern int prctl (int option, byte [] arg2, IntPtr arg3,
		                                 IntPtr arg4, IntPtr arg5);

		[DllImport ("libc")] // BSD
		private static extern void setproctitle (byte [] fmt, byte [] str_arg);

		public static void SetProcessName (string name)
		{
			if (OSName == "Linux") {

				if (prctl (15 /* PR_SET_NAME */, Encoding.ASCII.GetBytes (name + "\0"),
				           IntPtr.Zero, IntPtr.Zero, IntPtr.Zero) != 0) {
					throw new ApplicationException ("Error setting process name: " +
					                                Mono.Unix.Native.Stdlib.GetLastError ());
				}
			} else if (OSName == "FreeBSD") { // XXX: I'm not sure this is right
					setproctitle (Encoding.ASCII.GetBytes ("%s\0"),
					Encoding.ASCII.GetBytes (name + "\0"));
			}
		}
	}
}
