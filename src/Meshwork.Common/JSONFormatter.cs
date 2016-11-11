//
// JSONFormatter.cs
// 
// Copyright (C) 2009 Eric Butler
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Text;

namespace Meshwork.Common
{
	public static class JSONFormatter
	{
		static readonly char TAB_CHAR = '\t';

		public static string FormatJSON(string json)
		{
			var result = new StringBuilder();
			int tabCount = 0;
			bool inString = false;
			bool ignoreNext = false;

			for (int x = 0; x < json.Length; x++) {
				char thisChar = json[x];
				if (ignoreNext) {
					result.Append(thisChar);
					ignoreNext = false;
				} else {
					switch (thisChar) {
						case '{':
							result.Append(thisChar);
							if (!inString) {
								tabCount ++;
								result.Append(Environment.NewLine);
								result.Append(TAB_CHAR, tabCount);
							}
							break;
						case '}':
							if (!inString) {
								tabCount --;
								result.Append(Environment.NewLine);
								result.Append(TAB_CHAR, tabCount);
							}
							result.Append(thisChar);
							break;
						case ',':
							result.Append(thisChar);
							if (!inString) {
								result.Append(Environment.NewLine);
								result.Append(TAB_CHAR, tabCount);
							}
							break;
						case '[':
							result.Append(thisChar);
							if (!inString) {
								tabCount ++;
								result.Append(Environment.NewLine);
								result.Append(TAB_CHAR, tabCount);
							}
							break;
						case ']':
							if (!inString) {
								tabCount --;
								result.Append(Environment.NewLine);
								result.Append(TAB_CHAR, tabCount);
							}
							result.Append(thisChar);
							break;
						case '"':
							inString = !inString;
							result.Append(thisChar);
							break;
						case '\\':
							if (inString) {
								ignoreNext = true;
							}
							result.Append(thisChar);
							break;
						case ':':
							if (!inString) {
								result.Append(": ");
							} else {
								result.Append(thisChar);
							}
							break;
						case ' ':
							if (inString) {
								result.Append(thisChar);
							}
							break;
						default:
							result.Append(thisChar);
							break;
					}
				}
			}
			return result.ToString();
		} 
	}
}
