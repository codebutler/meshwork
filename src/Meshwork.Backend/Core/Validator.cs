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

namespace Meshwork.Backend.Core
{
	public class Validator
	{
		public static bool ValidNickname(string Name)
		{
		    if (StringIsEmpty(Name) || CheckForXmlChars(Name) || Name.Length > 30 || Name.IndexOf("@") > -1 || Name.IndexOf("+") > -1 || Name.IndexOf("%") > -1 || Name.IndexOf(" ") > -1) {
				return false;
			}
		    return true;
		}

		public static bool ValidName(string Name)
		{
		    if (StringIsEmpty(Name) || CheckForXmlChars(Name)) {
				return false;
			}
		    return true;
		}

		private static bool CheckForXmlChars(string Str)
		{
		    //if (Str.IndexOf("<") > -1 | Str.IndexOf(">") > -1 | Str.IndexOf(Microsoft.VisualBasic.Chr(34)) > -1) {
			if (Str.IndexOf("<") > -1 | Str.IndexOf(">") > -1 | Str.IndexOf("\"") > -1) {
				return true;
			}
		    return false;
		}

		private static bool StringIsEmpty(string str)
		{
		    if (str == null)
				return true;
		    if (str.Trim () == "")
		        return true;
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
			var pattern = new Regex("[a-z]");
			return pattern.IsMatch(Text);
		}

		public static bool HasUppercaseLetter(string Text) {
			var pattern = new Regex("[A-Z]");
			return pattern.IsMatch(Text);
		}

		public static bool HasNumber(string Text) {
			var pattern = new Regex("[\\d]");
			return pattern.IsMatch(Text);
		}

		public static bool HasPunctuation(string Text) {
			var pattern = new Regex("[\\W|_]");
			return pattern.IsMatch(Text);
		}
	}
}
