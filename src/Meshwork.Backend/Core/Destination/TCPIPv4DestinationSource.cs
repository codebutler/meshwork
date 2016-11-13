using System;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
    public class TCPIPv4DestinationSource : TCPIPDestinationSource
    {
        public TCPIPv4DestinationSource(Core core) : base(core)
        {
        }

        public override Type DestinationType { get; } = typeof(TCPIPv4Destination);

        public override Type ListenerType { get; } = typeof(TcpTransportListener);
    }
}