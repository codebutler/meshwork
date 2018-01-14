// 
// Carbon.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//       Geoff Norton  <gnorton@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace Meshwork.Client.GtkClient.Platform.Mac
{
	internal static class Carbon
	{
		public const string CarbonLib = "/System/Library/Frameworks/Carbon.framework/Versions/Current/Carbon";

		#region Internal Mac API for setting process name

		[DllImport(CarbonLib)]
		static extern int GetCurrentProcess(out ProcessSerialNumber psn);

		[DllImport(CarbonLib)]
		static extern int CPSSetProcessName(ref ProcessSerialNumber psn, string name);

		public static void SetProcessName(string name)
		{
			try
			{
				ProcessSerialNumber psn;
				if (GetCurrentProcess(out psn) == 0)
					CPSSetProcessName(ref psn, name);
			}
			catch { } //EntryPointNotFoundException?
		}

		struct ProcessSerialNumber
		{
#pragma warning disable 0169
			ulong highLongOfPSN;
			ulong lowLongOfPSN;
#pragma warning restore 0169
		}

		#endregion
	}
}