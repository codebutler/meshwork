//
// MessageIdCollection.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
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
			List<string> deleteList = new List<string>();

			lock (this) {
				foreach (KeyValuePair<string,DateTime> pair in this) {
					string key = pair.Key;
					DateTime dt = pair.Value;

					if (DateTime.Now.Subtract(dt).Milliseconds >= 6000) {
						deleteList.Add(key);
					}
				}
			}

			foreach (string key in deleteList) {
				this.Remove(key);
			}
		}
	}
}

