//
// IPage.cs:
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2007 FileFind.net
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
