//
// Object.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;

namespace Meshwork.Common
{
    [Obsolete]
	public class Object : object
	{
		Dictionary<string,object> properties;
		
		public Dictionary<string,object> Properties {
			get {
				if (properties == null) {
					properties = new Dictionary<string,object>();
				}
				return properties;
			}
		}
	}
}
