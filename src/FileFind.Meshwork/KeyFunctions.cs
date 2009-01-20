//
// KeyFunctions.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections;

namespace FileFind.Meshwork
{
	public class KeyFunctions
	{
		public static string MakePrivateKeyBlock(string Nickname, string ClientVersionString, string XmlPrivateKey)
		{
			string ReturnString = "-----BEGIN MESHWORK PRIVATE KEY BLOCK-----" + Environment.NewLine;
			ReturnString += "Nickname: " + Nickname + Environment.NewLine;
			if (ClientVersionString != null) {
				ReturnString += "Version: " + ClientVersionString + Environment.NewLine;
			}
			ReturnString += Environment.NewLine;
			ReturnString += FileFind.Common.AddLineBreaks(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(XmlPrivateKey)));
			ReturnString += Environment.NewLine;
			ReturnString += "-----END MESHWORK PRIVATE KEY BLOCK-----";
			return ReturnString;
		}

		public static string MakePublicKeyBlock(string Nickname, string ClientVersionString, string XmlPublicKey)
		{
			string ReturnString = "-----BEGIN MESHWORK PUBLIC KEY BLOCK-----" + Environment.NewLine;
			ReturnString += "Nickname: " + Nickname + Environment.NewLine;
			if (ClientVersionString != null) {
				ReturnString += "Version: " + ClientVersionString + Environment.NewLine;
			}
			ReturnString += Environment.NewLine;
			ReturnString += FileFind.Common.AddLineBreaks(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(XmlPublicKey)));
			ReturnString += Environment.NewLine;
			ReturnString += "-----END MESHWORK PUBLIC KEY BLOCK-----";
			return ReturnString;
		}

		public static PublicKey ParsePublicKeyBlock(string KeyBlock)
		{
			PublicKey Result = new PublicKey();
			string KeyString = "";
			object[] Lines = KeyBlock.Split(Environment.NewLine.ToCharArray());
			ArrayList NewLines = new ArrayList();
			for (int i = 0; i <= Lines.Length - 1; i++) {
				if (Lines[i].ToString().Trim() != "") {
					NewLines.Add(Lines[i]);
				}
			}
			Lines = ((object[])(NewLines.ToArray()));
			if (Lines[0].ToString().Trim() != "-----BEGIN MESHWORK PUBLIC KEY BLOCK-----") {
				throw new Exception("Invalid Key Format - Invalid or missing begin line");
			}
			if (Lines[Lines.Length - 1].ToString().Trim() != "-----END MESHWORK PUBLIC KEY BLOCK-----") {
				throw new Exception("Invalid Key Format - Invalid or missing end line");
			}
			for (int x = 1; x <= Lines.Length - 2; x++) {
				string ThisLine = Lines[x].ToString().Trim();
				if (ThisLine.StartsWith("Nickname:")) {
					Result.Identifier = ThisLine.Substring(10);
				} else if (ThisLine.StartsWith("Network:")) {
					Result.NetworkName = ThisLine.Substring(9);
				} else if (ThisLine.StartsWith("Version:")) {
					Result.Version = ThisLine.Substring(9);
				} else if (ThisLine != String.Empty) {
					KeyString += ThisLine;
				}
			}
			Result.Key = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(KeyString));
			if (Result.Identifier == null || Result.Identifier == "") {
				throw new Exception("Invalid/missing identifier");
			}
			if (Result.NetworkName == null || Result.NetworkName == "") {
				throw new Exception("Invalid/missing network name");
			}
			if (Result.Version == null || Result.Version == "") {
				throw new Exception("Invalid/missing client version");
			}
			if (Result.Key == null || Result.Key == "") {
				throw new Exception("Invalid/missing public key");
			}
			return Result;
		}

	}
}
