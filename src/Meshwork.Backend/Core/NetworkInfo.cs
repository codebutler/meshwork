//
// NetworkInfo.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System.Collections.Generic;
using Meshwork.Backend.Core.Protocol;
using Meshwork.Common.Serialization;

namespace Meshwork.Backend.Core
{
	public class NetworkInfo
	{
	    // FIXME: Should be a list/array.
	    public Dictionary<string, TrustedNodeInfo> TrustedNodes { get; } = new Dictionary<string, TrustedNodeInfo>();

	    // FIXME: readonly list!?
	    public List<MemoInfo> Memos { get; } = new List<MemoInfo>();

	    public string NetworkName { get; set; }

	    [DontSerialize]
	    public string NetworkId => Common.Utils.SHA512Str(NetworkName);
	}
}
