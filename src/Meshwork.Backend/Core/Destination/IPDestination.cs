//
// IPDestination.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System.Net;

namespace Meshwork.Backend.Core.Destination
{
    public abstract class IPDestination : DestinationBase
	{
	    protected IPDestination (Core core, IPAddress ip, uint port, bool isOpenExternally) : base(core)
		{
			IPAddress = ip;
			Port = port;
			IsOpenExternally = isOpenExternally;
		}

	    public IPAddress IPAddress { get; }

	    public uint Port { get; }

	    public override bool IsExternal => !Common.Common.IsInternalIP(IPAddress);
	}
}
