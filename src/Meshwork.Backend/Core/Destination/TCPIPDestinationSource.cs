using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Meshwork.Backend.Core.Transport;

namespace Meshwork.Backend.Core.Destination
{
    public abstract class TCPIPDestinationSource : IDestinationSource
    {
        private readonly Core core;
        private readonly List<IDestination> destinations = new List<IDestination>();

        public event DestinationEventHandler DestinationAdded;
        public event DestinationEventHandler DestinationRemoved;

        int listenPort = TcpTransport.DefaultPort;

        public IList<IDestination> Destinations {
            get { 
                return destinations.AsReadOnly();
            }
        }

        public abstract Type DestinationType {
            get;
        }

        public abstract Type ListenerType {
            get;
        }

        public int ListenPort {
            get {
                return listenPort;
            }
            set {
                listenPort = value;
            }
        }

        public TCPIPDestinationSource(Core core)
        {
            this.core = core;
            listenPort = core.Settings.TcpListenPort;

            // XXX: Use NetworkManager to support IP changes,
            // etc. without restarting Meshwork.

            var addresses = core.OS.GetInterfaceAddresses();
            foreach (var address in addresses) {

                // Ignore loopback.
                if (IPAddress.IsLoopback(address.Address))
                    continue;
				
                // Only include addresses of the correct type.
                if (this is TCPIPv4DestinationSource && address.Address.AddressFamily != AddressFamily.InterNetwork)
                    continue;
                else if (this is TCPIPv6DestinationSource && address.Address.AddressFamily != AddressFamily.InterNetworkV6)
                    continue;

                // XXX: Check for firewall. For now, we assume true because 99.99% of the
                // world is behind a NAT, the remaining 0.01% probably know what they are doing.
                var isOpenExternally = !Common.Common.IsInternalIP(address.Address);

                IDestination destination = null;

                if (this is TCPIPv6DestinationSource) {
                    var ip =  address.Address;
                    ip.ScopeId = 0;
                    destination = (IDestination)Activator.CreateInstance(DestinationType, core, address.IPv6PrefixLength, ip, (uint)listenPort, isOpenExternally);
                } else {
                    destination = (IDestination)Activator.CreateInstance(DestinationType, core, address.Address, (uint)listenPort, isOpenExternally);
                }

                destinations.Add(destination);

                DestinationAdded?.Invoke(destination);
            }
        }

        public void Update ()
        {
            throw new NotImplementedException();
        }
    }
}