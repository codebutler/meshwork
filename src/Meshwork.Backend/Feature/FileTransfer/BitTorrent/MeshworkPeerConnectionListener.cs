using System;
using System.Net;
using System.Text;
using Meshwork.Backend.Core;
using Meshwork.Backend.Core.Transport;
using MonoTorrent.Client;
using MonoTorrent.Client.Encryption;

namespace Meshwork.Backend.Feature.FileTransfer.BitTorrent
{
	internal class MeshworkPeerConnectionListener : PeerListener
	{
	    private readonly Core.Core core;

	    public MeshworkPeerConnectionListener (Core.Core core)
			: base (new IPEndPoint (IPAddress.Loopback, 0))
	    {
	        this.core = core;
	        // Nothing
	    }
		
		public override void Start()
		{
			// Nothing
		}
		
		public override void Stop()
		{
			// Nothing
		}			
		
		public void AddConnection (TorrentConnection connection, TorrentManager manager)
		{
			var remoteId = string.Empty;

			LoggingService.LogDebug("AddConnection(): Start");

			if (!connection.IsIncoming) {
				// Send my identity.
				// XXX: This absolutely needs to be signed.
				connection.Transport.SendMessage(Encoding.ASCII.GetBytes(core.MyNodeID));

				// Get other end's identity.
				var message = connection.Transport.ReceiveMessage();
				remoteId = Encoding.ASCII.GetString(message);

			} else {
				// Get other end's identity.
				var message = connection.Transport.ReceiveMessage();
				remoteId = Encoding.ASCII.GetString(message);

				// Send my identity.
				// XXX: This absolutely needs to be signed.
				connection.Transport.SendMessage(Encoding.ASCII.GetBytes(core.MyNodeID));
			}

			LoggingService.LogDebug("Pushing connection to engine: {0} - {1}", connection.IsIncoming ? "Incoming" : "Outgoing",
			                  ((TcpTransport)connection.Transport).RemoteEndPoint.ToString());

			var p = new Peer("", new Uri($"meshwork:{remoteId}"), EncryptionTypes.PlainText);
			RaiseConnectionReceived(p, connection, manager);

			LoggingService.LogDebug("AddConnection(): End");
		}
	}
}
