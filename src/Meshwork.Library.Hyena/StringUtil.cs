//
// StringUtil.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Meshwork.Library.Hyena
{    
    public static class StringUtil
    {
        private static CompareOptions compare_options = 
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace |
            CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;

        public static int RelaxedIndexOf (string haystack, string needle)
        {
            return CultureInfo.CurrentCulture.CompareInfo.IndexOf (haystack, needle, compare_options);
        }
        
        public static int RelaxedCompare (string a, string b)
        {
            if (a == null && b == null) {
                return 0;
            }
            if (a != null && b == null) {
                return 1;
            }
            if (a == null && b != null) {
                return -1;
            }

            var a_offset = a.StartsWith ("the ") ? 4 : 0;
            var b_offset = b.StartsWith ("the ") ? 4 : 0;

            return CultureInfo.CurrentCulture.CompareInfo.Compare (a, a_offset, a.Length - a_offset, 
                b, b_offset, b.Length - b_offset, compare_options);
        }
        
        public static string CamelCaseToUnderCase (string s)
        {
            return CamelCaseToUnderCase (s, '_');
        }
        
        private static Regex camelcase = new Regex ("([A-Z]{1}[a-z]+)", RegexOptions.Compiled);
        public static string CamelCaseToUnderCase (string s, char underscore)
        {
            if (string.IsNullOrEmpty (s)) {
                return null;
            }
        
            var undercase = new StringBuilder ();
            var tokens = camelcase.Split (s);
            
            for (var i = 0; i < tokens.Length; i++) {
                if (tokens[i] == string.Empty) {
                    continue;
                }

                undercase.Append (tokens[i].ToLower (CultureInfo.InvariantCulture));
                if (i < tokens.Length - 2) {
                    undercase.Append (underscore);
                }
            }
            
            return undercase.ToString ();
        }

        public static string UnderCaseToCamelCase (string s)
        {
            if (string.IsNullOrEmpty (s)) {
                return null;
            }

            var builder = new StringBuilder ();

            for (int i = 0, n = s.Length, b = -1; i < n; i++) {
                if (b < 0 && s[i] != '_') {
                    builder.Append (char.ToUpper (s[i]));
                    b = i;
                } else if (s[i] == '_' && i + 1 < n && s[i + 1] != '_') {
                    if (builder.Length > 0 && char.IsUpper (builder[builder.Length - 1])) {
                        builder.Append (char.ToLower (s[i + 1]));
                    } else {
                        builder.Append (char.ToUpper (s[i + 1]));
                    }
                    i++;
                    b = i;
                } else if (s[i] != '_') {
                    builder.Append (char.ToLower (s[i]));
                    b = i;
                }
            }

            return builder.ToString ();
        }

        public static string RemoveNewlines (string input)
        {
            if (input != null) {
                return input.Replace ("\r\n", string.Empty).Replace ("\n", string.Empty);
            }
            return null;
        }

        private static Regex tags = new Regex ("<[^>]+>", RegexOptions.Compiled | RegexOptions.Multiline);
        public static string RemoveHtml (string input)
        {
            if (input == null) {
                return input;
            }

            return tags.Replace (input, string.Empty);
        }

        public static string DoubleToTenthsPrecision (double num)
        {
            return DoubleToTenthsPrecision (num, false);
        }
        
        public static string DoubleToTenthsPrecision (double num, bool always_decimal)
        {
            return DoubleToTenthsPrecision (num, always_decimal, NumberFormatInfo.CurrentInfo);
        }

        public static string DoubleToTenthsPrecision (double num, bool always_decimal, IFormatProvider provider)
        {
            num = Math.Round (num, 1, MidpointRounding.ToEven);
            return string.Format (provider, !always_decimal && num == (int)num ? "{0:N0}" : "{0:N1}", num);
        }
        
        // This method helps us pluralize doubles. Probably a horrible i18n idea.
        public static int DoubleToPluralInt (double num)
        {
            if (num == (int)num)
                return (int)num;
            return (int)num + 1;
        }
        
        // A mapping of non-Latin characters to be considered the same as
        // a Latin equivalent.
        private static Dictionary<char, char> BuildSpecialCases ()
        {
            var dict = new Dictionary<char, char> ();
            dict['\u00f8'] = 'o';
            dict['\u0142'] = 'l';
            return dict;
        }
        private static Dictionary<char, char> searchkey_special_cases = BuildSpecialCases ();
        
        //  Removes accents from Latin characters, and some kinds of punctuation.
        public static string SearchKey (string val)
        {
            if (string.IsNullOrEmpty (val)) {
                return val;
            }
            
            val = val.ToLower ();
            var sb = new StringBuilder ();
            UnicodeCategory category;
            var previous_was_latin = false;
            var got_space = false;
            
            // Normalizing to KD splits into (base, combining) so we can check for Latin
            // characters and then strip off any NonSpacingMarks following them
            foreach (var orig_c in val.TrimStart ().Normalize (NormalizationForm.FormKD)) {
                
                // Check for a special case *before* whitespace. This way, if
                // a special case is ever added that maps to ' ' or '\t', it
                // won't cause a run of whitespace in the result.
                var c = orig_c;
                if (searchkey_special_cases.ContainsKey (c)) {
                    c = searchkey_special_cases[c];
                }
                
                if (c == ' ' || c == '\t') {
                    got_space = true;
                    continue;
                }
                
                category = char.GetUnicodeCategory (c);
                if (category == UnicodeCategory.OtherPunctuation) {
                    // Skip punctuation
                } else if (!(previous_was_latin && category == UnicodeCategory.NonSpacingMark)) {
                    if (got_space) {
                        sb.Append (" ");
                        got_space = false;
                    }
                    sb.Append (c);
                }
                
                // Can ignore A-Z because we've already lowercased the char
                previous_was_latin = (c >= 'a' && c <= 'z');
            }

            var result = sb.ToString ();
            try {
                result = result.Normalize (NormalizationForm.FormKC);
            }
            catch {
                // FIXME: work-around, see http://bugzilla.gnome.org/show_bug.cgi?id=590478
            }
            return result;
        }
        
        private static Regex invalid_path_regex = BuildInvalidPathRegex ();

        private static Regex BuildInvalidPathRegex ()
        {
            char [] invalid_path_characters = {
                // Control characters: there's no reason to ever have one of these in a track name anyway,
                // and they're invalid in all Windows filesystems.
                '\x00', '\x01', '\x02', '\x03', '\x04', '\x05', '\x06', '\x07',
                '\x08', '\x09', '\x0A', '\x0B', '\x0C', '\x0D', '\x0E', '\x0F',
                '\x10', '\x11', '\x12', '\x13', '\x14', '\x15', '\x16', '\x17',
                '\x18', '\x19', '\x1A', '\x1B', '\x1C', '\x1D', '\x1E', '\x1F',
                
                // Invalid in FAT32 / NTFS: " \ / : * | ? < >
                // Invalid in HFS   :
                // Invalid in ext3  /
                '"', '\\', '/', ':', '*', '|', '?', '<', '>'
            };

            var regex_str = "[";
            for (var i = 0; i < invalid_path_characters.Length; i++) {
                regex_str += "\\" + invalid_path_characters[i];
            }
            regex_str += "]+";
            
            return new Regex (regex_str, RegexOptions.Compiled);
        }
        
        private static CompareInfo culture_compare_info = CultureInfo.CurrentCulture.CompareInfo;
        public static byte[] SortKey (string orig)
        {
            if (orig == null) { return null; }
            return culture_compare_info.GetSortKey (orig, CompareOptions.IgnoreCase).KeyData;
        }

        private static readonly char[] escape_path_trim_chars = {'.', '\x20'};
        public static string EscapeFilename (string input)
        {
            if (input == null)
                return "";
            
            // Remove leading and trailing dots and spaces.
            input = input.Trim (escape_path_trim_chars);

            return invalid_path_regex.Replace (input, "_");
        }

        public static string EscapePath (string input)
        {
            if (input == null)
                return "";

            // This method should be called before the full path is constructed.
            if (Path.IsPathRooted (input)) {
                return input;
            }

            var builder = new StringBuilder ();
            foreach (var name in input.Split (Path.DirectorySeparatorChar)) {
                // Escape the directory or the file name.
                var escaped = EscapeFilename (name);
                // Skip empty names.
                if (escaped.Length > 0) {
                    builder.Append (escaped);
                    builder.Append (Path.DirectorySeparatorChar);
                }
            }

            // Chop off the last character.
            if (builder.Length > 0) {
                builder.Length--;
            }

            return builder.ToString ();
        }
        
        public static string MaybeFallback (string input, string fallback)
        {
            var trimmed = input == null ? null : input.Trim ();
            return string.IsNullOrEmpty (trimmed) ? fallback : trimmed;
        }
        
        public static uint SubstringCount (string haystack, string needle)
        {
            if (string.IsNullOrEmpty (haystack) || string.IsNullOrEmpty (needle)) {
                return 0;
            }
            
            var position = 0;
            uint count = 0;
            while (true) {
                var index = haystack.IndexOf (needle, position);
                if (index < 0) {
                    return count;
                }
                count++;
                position = index + 1;
            }
        }
    }
}
