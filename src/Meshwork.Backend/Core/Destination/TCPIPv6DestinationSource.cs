using System;

namespace Meshwork.Backend.Core.Destination
{
    public class TCPIPv6DestinationSource : TCPIPDestinationSource
    {
        public TCPIPv6DestinationSource(Core core) : base(core)
        {
        }

        public override Type DestinationType => typeof(TCPIPv6Destination);

        // We piggyback on TCPIPv4DestinationSource's listener.
        public override Type ListenerType => null;
    }
}