//
// OverlayTransport.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Net;

namespace FileFind.Meshwork.Transport
{
	public class OverlayTransport : AbstractTransport
	{
		TransportCallback callback;
		Node remoteNode;
		string connectionId;

		public OverlayTransport (Node node)
		{
			base.transportState = TransportState.Waiting;
			base.incoming = false;
			remoteNode = node;
		}

		public OverlayTransport (Node node, string connectionId)
		{
			base.transportState = TransportState.Connected;
			base.incoming = true;
			remoteNode = node;
			this.connectionId = connectionId;
		}

		public override EndPoint RemoteEndPoint {
			get {
				return null;
			}
		}

		public override void Send (byte[] buffer) 
		{
			this.Send (buffer, 0, buffer.Length);
		}

		public override void Send (byte[] buffer, int offset, int size)
		{
			byte[] sendBuffer = new byte[size];
			Array.Copy (buffer, 0, sendBuffer, offset, size);

			Message message = base.Network.MessageBuilder.CreateTransportDataMessage (remoteNode, connectionId, sendBuffer);
			base.Network.SendRoutedMessage (message);
		}

		public override void Connect (TransportCallback callback)
		{
			Message message = base.Network.MessageBuilder.CreateTransportConnectMessage (remoteNode, connectionId);
			base.Network.SendRoutedMessage (message);
			
			// Wait for a confirmation now.
			// MessageProcessor will call ConnectionAccepted
			this.callback = callback;
		}

		public override void Receive (byte[] buffer)
		{
			Receive (buffer, 0, buffer.Length);
		}

		public override void Receive (byte[] buffer, int offset, int size)
		{
			throw new NotImplementedException();
		}

		public override void BeginReceive (byte[] buffer, int offset, int size, ReceiveDataCallback callback)
		{
			throw new NotImplementedException();
		}

		public override void SendEncrypted (byte[] buffer, int offset, int size)
		{
			buffer = base.Encrypt (buffer, offset, size);
			Send (buffer, 0, buffer.Length);
		}

		public override void ReceiveEncrypted (byte[] buffer, int offset, int size)
		{
			Receive (buffer, offset, size);
			buffer = base.Decrypt (buffer, 0, buffer.Length);
		}

		public override void BeginSend (byte[] buffer, int offset, int size, TransportCallback callback)
		{
			throw new NotImplementedException();
		}

		public override void BeginSendEncrypted (byte[] buffer, int offset, int size, TransportCallback callback)
		{
			throw new NotImplementedException();
		}

		public override void BeginReceiveEncrypted (byte[] buffer, int offset, int size, ReceiveDataCallback callback)
		{
			throw new NotImplementedException();
		}

		public override void Disconnect ()
		{
			Disconnect (null);
		}

		public override void Disconnect (Exception ex)
		{
			base.transportState = TransportState.Disconnected;

			if (ex != null)
				LogManager.Current.WriteToLog (ex);
			else
				LogManager.Current.WriteToLog (String.Format ("Transport {0} disconnected", this.ToString()));

			base.RaiseDisconnected (ex);
		}

		internal void ConnectionAccepted ()
		{
			callback(this);
			base.transportState = TransportState.Connected;
		}
	}
}
