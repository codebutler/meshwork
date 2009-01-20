//
// Object.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System.Collections.Generic;

namespace FileFind.Meshwork
{
	public class Object : System.Object
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
