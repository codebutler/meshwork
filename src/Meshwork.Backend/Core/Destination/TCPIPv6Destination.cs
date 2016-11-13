using System.Net;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
    public class TCPIPv6Destination : IPv6Destination
    {	
        static TCPIPv6Destination ()
        {
            DestinationTypeFriendlyNames.RegisterFriendlyName(typeof(TCPIPv6Destination), "TCP (IPv6)");
        }

        public TCPIPv6Destination(Core core, DestinationInfo info) 
            : base(core, int.Parse(info.Data[1]), IPAddress.Parse(info.Data[0]), uint.Parse(info.Data[2]), info.IsOpenExternally)
        {
        }

        public TCPIPv6Destination (Core core, int prefixLength, IPAddress ip, uint port, bool isOpenExternally) 
            : base (core, prefixLength, ip, port, isOpenExternally)
        {
        }

        public override ITransport CreateTransport (ulong connectionType)
        {
            return new TcpTransport(IPAddress, (int)Port, connectionType);
        }

        public override string ToString () => $"[{IPAddress}/{PrefixLength}]:{Port}";
    }
}