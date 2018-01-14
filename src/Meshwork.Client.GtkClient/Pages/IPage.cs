//
// IPage.cs:
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2007 Meshwork Authors
// 

using System;

namespace Meshwork.Client.GtkClient.Pages
{
	public interface IPage
	{
		event EventHandler UrgencyHintChanged;

		bool UrgencyHint {
			get;
		}
	}
}
