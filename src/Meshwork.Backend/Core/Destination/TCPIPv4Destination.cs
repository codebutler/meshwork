//
// TCPDestination.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2008 Meshwork Authors
//

using System.Net;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
    public class TCPIPv4Destination : IPv4Destination
	{
		static TCPIPv4Destination ()
		{
			DestinationTypeFriendlyNames.RegisterFriendlyName(typeof(TCPIPv4Destination), "TCP");
		}

		public TCPIPv4Destination (Core core, DestinationInfo info)
		    : base(core, IPAddress.Parse(info.Data[0]), uint.Parse(info.Data[1]), info.IsOpenExternally)
		{
		}

		public TCPIPv4Destination (Core core, IPAddress ip, uint port, bool isOpenExternally)
		    : base (core, ip, port, isOpenExternally)
		{
		}

		public override ITransport CreateTransport (ulong connectionType)
		{
			return new TcpTransport(IPAddress, (int)Port, connectionType);
		}

		public override string ToString () => $"{IPAddress}:{Port}";
	}
}
