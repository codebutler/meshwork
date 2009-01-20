//
// Validator.cs: Validate various things
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Text.RegularExpressions;

namespace FileFind.Meshwork.Security
{
	public class Validator
	{
		public static bool ValidNickname(string Name) {
			if (StringIsEmpty(Name) == true || CheckForXmlChars(Name) == true || Name.Length > 30 || Name.IndexOf("@") > -1 || Name.IndexOf("+") > -1 || Name.IndexOf("%") > -1 || Name.IndexOf(" ") > -1) {
				return false;
			} else {
				return true;
			}
		}

		public static bool ValidName(string Name) {
			if (StringIsEmpty(Name) == true || CheckForXmlChars(Name) == true) {
				return false;
			} else {
				return true;
			}
		}

		private static bool CheckForXmlChars(string Str) {
			//if (Str.IndexOf("<") > -1 | Str.IndexOf(">") > -1 | Str.IndexOf(Microsoft.VisualBasic.Chr(34)) > -1) {
			if (Str.IndexOf("<") > -1 | Str.IndexOf(">") > -1 | Str.IndexOf("\"") > -1) {
				return true;
			} else {
				return false;
			}
		}

		private static bool StringIsEmpty(string str)
		{
			if (str == null)
				return true;
			else if (str.Trim () == "")
				return true;
			else
				return false;
		}

		// XXX: This is horrible, implement cracklib or something
		public static bool IsPassSecure(string Password)
		{
			if (Password.Length < 8) {
				throw new Exception("Your password must be at least 8 characters long.");
			}
			if (HasLowercaseLetter(Password) == false) {
				throw new Exception("Your password must contain at leaast 1 lowercase letter.");
			}
			if (HasUppercaseLetter(Password) == false) {
				throw new Exception("Your password must contain at leaast 1 uppercase letter.");
			}
			if (HasNumber(Password) == false) {
				throw new Exception("Your password must contain at leaast 1 number.");
			}
			if (HasPunctuation(Password) == false) {
				throw new Exception("Your password must contain at leaast 1 non letter/number (punctuation).");
			}
			return true;
		}

		public static bool HasLowercaseLetter(string Text) {
			Regex pattern = new Regex("[a-z]");
			return pattern.IsMatch(Text);
		}

		public static bool HasUppercaseLetter(string Text) {
			Regex pattern = new Regex("[A-Z]");
			return pattern.IsMatch(Text);
		}

		public static bool HasNumber(string Text) {
			Regex pattern = new Regex("[\\d]");
			return pattern.IsMatch(Text);
		}

		public static bool HasPunctuation(string Text) {
			Regex pattern = new Regex("[\\W|_]");
			return pattern.IsMatch(Text);
		}
	}
}
