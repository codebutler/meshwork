//
// Serialization.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
//

using System;
using Newtonsoft.Json;

namespace Meshwork.Common.Serialization
{
	public static class Json
	{
		public static string Serialize (object obj)
		{
			return JsonConvert.SerializeObject(obj);
		}

	    public static object Deserialize (string json, Type type)
	    {
	        return JsonConvert.DeserializeObject(json, type);
	    }
	}
}
