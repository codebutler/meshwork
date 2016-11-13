using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Meshwork.Common;

namespace Meshwork.Backend.Core.Destination
{
    public abstract class IPv6Destination : IPDestination
    {
        protected IPv6Destination (Core core, int prefixLength, IPAddress ip, uint port, bool isOpenExternally)
            : base (core, ip, port, isOpenExternally)
        {
            if (ip.AddressFamily != AddressFamily.InterNetworkV6) {
                throw new ArgumentException("ip must be IPv6");
            }

            PrefixLength = prefixLength;
        }

        public string NetworkPrefix => IPv6Util.GetNetworkPrefix(PrefixLength, IPAddress);

        public int PrefixLength { get; }

        public override bool CanConnect {
            get {
                if (!Common.Common.SupportsIPv6) {
                    return false;
                }
				
                if (IsExternal) {
                    return IsOpenExternally && Core.HasExternalIPv6;
                }

                // Can't connect to link-local addresses if no interface is set.
                if (Core.Settings.IPv6LinkLocalInterfaceIndex == -1) {
                    return false;
                }

                foreach (var destination in Core.DestinationManager.Destinations)
                {
                    // If this is an IPv6 address, we can connect only if
                    // one of our Destinations is also IPv6 and has the
                    // same network prefix (excluding link-local).
                    var pv6Destination = destination as IPv6Destination;
                    if (pv6Destination != null && destination.IsExternal) {
                        //IPv6Destination ipv6Destination = (IPv6Destination)destination;
                        if (NetworkPrefix == pv6Destination.NetworkPrefix) {
                            return true;
                        }

                        // In many (most?) cases, two nodes on the same LAN will
                        // have only link-local IPv6 addresses. If we have a
                        // matching external IPv4 address (i.e., we are behind
                        // the same IPv4 NAT router), then we can connect.
                    }
                    else if (destination is IPv4Destination && destination.IsExternal)
                    {
                        var myAddress = ((IPv4Destination)destination).IPAddress;
                        if (ParentList.Any(d => d.IsExternal && d is IPv4Destination && myAddress.Equals(((IPv4Destination)d).IPAddress)))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        public override DestinationInfo CreateDestinationInfo ()
        {
            var info = new DestinationInfo();
            info.IsOpenExternally = IsOpenExternally;
            info.TypeName = GetType().ToString();
            info.Data = new[] {
                IPAddress.ToString(),
                PrefixLength.ToString(),
                Port.ToString()
            };
            return info;
        }
		
        public override int CompareTo (IDestination other)
        {
            if (other is IPv6Destination)
            {
                // Prefer internal IP addresses
                if (IsExternal && !other.IsExternal) {
                    return 1;
                }
                if (!IsExternal && other.IsExternal) {
                    return -1;
                }
            } else if (other is IPv4Destination) {
                // Prefer IPv6 to IPv4
                return 1;
            }
            return 0;
        }
    }
}