//
// Utils.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Meshwork.Common
{
	public static class Utils
	{
		public static bool IsValidEmail (string inputEmail)
		{
			var strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
			  @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
			  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
			var re = new Regex (strRegex);
			if (re.IsMatch (inputEmail))
				return (true);
		    return (false);
		}	


		public static ulong GetUnixTimestamp()
		{
     			var t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
     			return (ulong)t.TotalSeconds;
		}

		public static DateTime ParseUnixTimestamp (ulong time)
		{
			var epoch = new DateTime(1970, 1, 1);
			return epoch.AddSeconds(time);
		}
		
		public static bool IsNumeric(string N) {
			try {
				long.Parse(N);
			} catch (Exception) {
				return false;
			}
			return true;
		}
		
		public static bool SupportsIPv6 {
			get {
				// XXX: This doesnt work? return Socket.OSSupportsIPv6;
				try {
					var tmp = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
					tmp.Close();
					return true;
				}
				catch { 
					return false;
				}
			}
		}

		public static string BytesToString(byte[] b) {
			string tmpstr = null;
			if (!(b == null)) {
				for (var x = 0; x <= b.Length - 1; x++) {
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
			
			var result = new byte[str.Length / 2];
			
			for (var x = 0; x < str.Length; x += 2) {
				result[x/2] = byte.Parse(str.Substring(x, 2), NumberStyles.HexNumber);
			}
			
			return result;
		}

		public static string MD5(byte[] bytesIn) {
			var bytesOut = new MD5CryptoServiceProvider().ComputeHash(bytesIn);
			var stringOut = BitConverter.ToString(bytesOut).Replace("-", string.Empty);
			return stringOut.ToLower ();
		}

		public static string MD5(string stringIn) {
			var bytesIn = Encoding.UTF8.GetBytes(stringIn);
			var bytesOut = new MD5CryptoServiceProvider().ComputeHash(bytesIn);
			var stringOut = BitConverter.ToString(bytesOut).Replace("-", string.Empty);
			return stringOut.ToLower ();
		}

		public static string SHA(byte[] bytesIn) {
			var SHAObj = SHA1.Create();
			var bytesOut = SHAObj.ComputeHash(bytesIn);
			var strOut = BitConverter.ToString(bytesOut).Replace("-","");
			return strOut.ToLower ();
		}

		public static byte[] SHA512 (string stringIn) 
		{
			SHA512 shaManaged = new SHA512Managed ();
			return shaManaged.ComputeHash (Encoding.ASCII.GetBytes (stringIn));
		}
		 

		public static string SHA512Str (string stringIn)
		{
			return BitConverter.ToString(SHA512(stringIn)).Replace("-","");
		}
		
		public static string SHA512Str (byte[] bytesIn)
		{
			SHA512 shaManaged = new SHA512Managed();
			return BitConverter.ToString(shaManaged.ComputeHash(bytesIn));
		}
		
		public static string AddLineBreaks(string strIn)
		{
			var strOut = "";
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
				var bytes = address.GetAddressBytes();
				return (bytes[0] == 10) || (bytes[0] == 192 && bytes[1] == 168) ||
				       (bytes[0] == 172 && (bytes[1] >= 16 && bytes[1] <= 31)) || 
				       (bytes[0] == 169 && bytes[1] == 254);
			}
		    if (address.AddressFamily == AddressFamily.InterNetworkV6) {
		        return address.IsIPv6LinkLocal;
		    }
		    throw new ArgumentException("address must be IPv4 or IPv6");
		}

		public static string FormatFingerprint (string fingerprint)
		{
			return fingerprint.CutIntoSetsOf(8).Join(" ");
		}
		
		public static string FormatFingerprint (string fingerprint, int sectionsPerLine)
		{
			return fingerprint.CutIntoSetsOf(8).EnumSlice(sectionsPerLine).Select(x => x.Join(" ")).Join("\n");
		}
		                                        
		
		public static string FormatBytes (decimal bytes)
		{
		    if (bytes >= 1099511627776)
				return Math.Round((bytes / 1024 / 1024 / 1024 / 1024),2) + " TB";
		    if (bytes >= 1073741824)
		        return Math.Round((bytes / 1024 / 1024 / 1024),2) + " GB";
		    if (bytes >= 1048576)
		        return Math.Round((bytes / 1024 / 1024),2) + " MB";
		    if (bytes >= 1024)
		        return Math.Round((bytes / 1024),2) + " KB";
		    if (bytes < 1024)
		        return bytes + " Bytes";
		    return "0 Bytes";
		}

		public static string FormatNumber (long number)
		{
			var nfi = new CultureInfo( "en-US", false ).NumberFormat;
			nfi.NumberDecimalDigits = 0;
			return number.ToString("N",nfi);
		}

		public static bool WildcardMatch (string text, string pattern)
		{
			return Regex.Match(text, ToRegexPattern(pattern)).Success;
		}

		/* From the mono class library source */
		/* /mcs/class/System.Web/System.Web.Configuration/HandlerItem.cs */
		private static string ToRegexPattern (string dosPattern)
		{
                        var result = dosPattern.Replace (".", "\\.");
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

			var suffixes = new Dictionary<string,string>();
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
			var match = Regex.Match(str, @"^(\d+)([A-Za-z]+)$");
			if (match.Success) {
				if (ulong.TryParse(match.Groups[1].Captures[0].Value.Trim(), out num)) {
					unitName = match.Groups[2].Captures[0].Value.Trim();
				}
			} else {
				// If all numeric, default to megabytes
				match = Regex.Match(str, @"^(\d+)$");
				if (match.Success) {
					if (ulong.TryParse(match.Groups[1].Captures[0].Value, out num)) {
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
			}
		    unitName = null;
		    return false;
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
			}
		    return 0;
		}
	}
}
