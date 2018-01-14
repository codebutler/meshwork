//
// MessageIdCollection.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System;
using System.Collections.Generic;

namespace Meshwork.Backend.Core.Collections
{
	public class MessageIdCollection : Dictionary<string, DateTime>
	{
		public void Add (string messageID)
		{
			if (messageID == null) {
				throw new ArgumentNullException("messageID");
			}

			base.Add(messageID, DateTime.Now);
		}

		public void Purge()
		{
			var deleteList = new List<string>();

			lock (this) {
				foreach (var pair in this) {
					var key = pair.Key;
					var dt = pair.Value;

					if (DateTime.Now.Subtract(dt).Milliseconds >= 6000) {
						deleteList.Add(key);
					}
				}
			}

			foreach (var key in deleteList) {
				Remove(key);
			}
		}
	}
}

