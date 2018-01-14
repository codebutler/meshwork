//
// TcpTransport.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Meshwork.Backend.Core.Transport
{
	public class TcpTransport : TransportBase
	{
		public static readonly int DefaultPort = 7332;

	    private readonly Core core;
		Socket socket;
		IPAddress address = IPAddress.Any;
		int port;
		TransportCallback connectCallback;

		object sendLock = new object();
		object receiveLock = new object();

		internal TcpTransport (Core core, Socket socket)
		{
		    this.core = core;
			this.socket = socket;
			address = (socket.RemoteEndPoint as IPEndPoint).Address;
			port = (socket.RemoteEndPoint as IPEndPoint).Port;
			incoming = true;
			transportState = TransportState.Connected;
			RaiseConnected();
		}

		public TcpTransport (IPAddress address, int port, ulong connectionType)
		{
			this.address = address;
			this.port = port;
			this.connectionType = connectionType;
			incoming = false;
			transportState = TransportState.Waiting;
		}

		public override void Connect (TransportCallback callback)
		{
			if (socket != null)
				throw new InvalidOperationException("This socket is already connected.");
			
			if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.None) || port == 0)
				throw new Exception("Invalid IP Address/Port");
			
			transportState = TransportState.Connecting;
			
			connectCallback = callback;
			
			if (address.IsIPv6LinkLocal) {
				address.ScopeId = core.Settings.IPv6LinkLocalInterfaceIndex;
			}
			
			var remoteEndpoint = new IPEndPoint(address, port);
			socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			socket.BeginConnect(remoteEndpoint, OnConnected, null);
		}

		public override int Send (byte[] buffer, int offset, int size)
		{
			lock (sendLock) {
				var totalSent = 0;
				while (totalSent < size) {
					var sent = socket.Send(buffer, offset + totalSent, size - totalSent, SocketFlags.None);
					if (sent == 0) {
						throw new Exception("No data was sent.");
					}
					totalSent += sent;
				}
				if (totalSent > size) {
					throw new Exception("Sent too much! " + totalSent + " " + size);
				}
				return totalSent;
			}
		}

		public override int Receive (byte[] buffer, int offset, int size)
		{
			if (size <= 0) {
				throw new ArgumentException("Cannot receive <= 0 bytes");
			}
			
			var totalReceived = 0;
			while (totalReceived < size) {
				var count = 0;
				if (socket == null) {
					// We were disconnected!
					return 0;
				}
				lock (receiveLock) {
					count = socket.Receive(buffer, offset + totalReceived, size - totalReceived, SocketFlags.None);
				}
				if (count == 0) {
					// This means the connection was closed.
					Disconnect();
					return 0;
				}
			    totalReceived += count;
					
			    if (totalReceived > size) {
			        throw new Exception("Somehow received too much! This shouldn't ever happen!");
			    }
			}
			return totalReceived;
		}

		public override EndPoint RemoteEndPoint {
			get
			{
			    if (socket != null && socket.Connected) {
					try {
						return socket.RemoteEndPoint;
					} catch (SocketException ex) {
						LoggingService.LogError("Failed to get remote end point. I am pretty sure this is a bug in mono!", ex);
						return new IPEndPoint(address, port);
					}
				}
			    return new IPEndPoint(address, port);
			}
		}

		public override string ToString ()
		{
			var builder = new StringBuilder();
			builder.Append("TCP/");

			if (Incoming)
				builder.Append("INCOMING/");
			else
				builder.Append("OUTGOING/");

			var addr = (socket != null) ? (socket.RemoteEndPoint as IPEndPoint).Address : address;
			if (addr.AddressFamily == AddressFamily.InterNetworkV6) {
				builder.Append("[");
				builder.Append(addr);
				builder.Append("]");
			} else
				builder.Append(addr);

			builder.Append(":");
			builder.Append(port.ToString());

			return builder.ToString();
		}

		public override void Disconnect ()
		{
			Disconnect(null);
		}

		public override void Disconnect (Exception ex)
		{
			if (transportState != TransportState.Disconnected) {
				transportState = TransportState.Disconnected;
				
				if (socket != null) {
					socket.Close();
					socket = null;
				}
				
				if (ex != null)
					if (ex is SocketException)
						LoggingService.LogInfo("Transport {0} disconnected ({1}).", ToString(), ex.Message);
					else
						LoggingService.LogInfo("Transport {0} disconnected with error: {1}", ToString(), ex.ToString());
				else
					LoggingService.LogInfo("Transport {0} disconnected", ToString());
				
				RaiseDisconnected(ex);
			}
		}

		private void OnConnected (IAsyncResult result)
		{
			try {
				socket.EndConnect(result);
				transportState = TransportState.Connected;
				RaiseConnected();
				connectCallback(this);
			} catch (Exception ex) {
				Disconnect(ex);
			}
		}
	}
}
