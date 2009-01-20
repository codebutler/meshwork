//
// UdpTransport.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.Runtime.Remoting.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace FileFind.Meshwork.Transport
{
	public class UdpTransport : AbstractTransport, IEncryptedTransport
	{
		IPEndPoint remoteEndPoint;
		TransportCallback connectCallback;

		// incoming
		internal UdpTransport (IPEndPoint remoteEndPoint)
		{
			this.remoteEndPoint = remoteEndPoint;
			base.incoming = true;
			base.transportState = TransportState.Connected;
		}

		// outgoing
		internal UdpTransport (IPAddress address, int port, ulong connectionType)
		{
			if (address.Equals (IPAddress.Any) || address.Equals (IPAddress.None) || port == 0)
				throw new Exception ("Invalid IP Address/Port");

			this.remoteEndPoint = new IPEndPoint (address, port);
			base.connectionType = connectionType;
			base.incoming = false;
			base.transportState = TransportState.Waiting;
		}

		// this is rather fake...
		public override void Connect (TransportCallback callback)
		{
			try {
				base.transportState = TransportState.Connecting;
				connectCallback = callback;
			
				base.transportState = TransportState.Connected;

				connectCallback (this);
			} catch (Exception ex) {
				Disconnect (ex);
			}
		}
	
		public override void SendEncrypted (byte[] buffer, int offset, int size)
		{
			buffer = base.Encrypt (buffer, offset, size);
			Send (buffer, 0, buffer.Length);
		}
		
		public override void Send (byte[] buffer)
		{
			Send (buffer, 0, buffer.Length);
		}

		public override void Send (byte[] buffer, int offset, int size)
		{
			socket.Send (buffer, offset, size, SocketFlags.None);
		}

		public override void ReceiveEncrypted (byte[] buffer, int offset, int size)
		{
			Receive (buffer, offset, size);
			buffer = base.Decrypt (buffer, 0, buffer.Length);
		}

		public override void Receive (byte[] buffer)
		{
			Receive (buffer, 0, buffer.Length);
		}
		
		public override void Receive (byte[] buffer, int offset, int size)
		{
			ReceivePrivate (buffer, offset, size, 0);
		}

		private void ReceivePrivate (byte[] buffer, int offset, int size, int totalReceived)
		{
			int count = socket.Receive (buffer, offset + totalReceived, size - totalReceived, SocketFlags.None);
			totalReceived += count;

			if (count == 0)
				Disconnect ();
    		        else if (count < size)
				ReceivePrivate (buffer, offset, size, totalReceived);
			else if (count > size)
				throw new Exception ("RECEIVED TOO MUCH SHIT!! GOT: " + count.ToString() + " WANTED " + size);
		}


		public override void BeginSendEncrypted (byte[] buffer, int offset, int size, TransportCallback callback)
		{
			buffer = base.Encrypt (buffer, offset, size);
			BeginSend (buffer, 0, buffer.Length, callback);
		}


		public override void BeginSend (byte[] buffer, int offset, int size, TransportCallback callback)
		{
			if (socket == null)
				throw new Exception ("Socket is disconnected.");

			AsyncCallback internalCallback = new AsyncCallback (OnDataSent);
			socket.BeginSend (buffer, offset, size, SocketFlags.None, internalCallback, callback);
		}

		public override void BeginReceiveEncrypted (byte[] buffer, int offset, int size, ReceiveDataCallback callback)
		{
			AsyncCallback internalCallback = new AsyncCallback (OnReceiveEncryptedData);
			socket.BeginReceive (buffer, offset, size, SocketFlags.None, internalCallback, new object[] {buffer, callback, size});
		}

		public override void BeginReceive (byte[] buffer, int offset, int size, ReceiveDataCallback callback)
		{
			BeginReceivePrivate (buffer, offset, size, callback, 0);
		}
		
		private void BeginReceivePrivate (byte[] buffer, int offset, int size, ReceiveDataCallback callback, int totalReceived)
		{
			AsyncCallback internalCallback = new AsyncCallback (OnReceivedData);
			socket.BeginReceive (buffer, offset + totalReceived, size - totalReceived, SocketFlags.None, internalCallback, 
					new object[] {buffer, offset, size, callback, totalReceived});
		}
		
		private void OnDataSent (IAsyncResult result)
		{
			try {
				socket.EndSend (result);
				TransportCallback callback = (TransportCallback) result.AsyncState;
				callback (this);
			} catch (Exception ex) {
				Disconnect (ex);
			}
		}

		private void OnReceiveEncryptedData (IAsyncResult result)
		{
			try {
				int bytesReceived = socket.EndReceive (result);
				int bytesExpected = Convert.ToInt32 (((object[])result.AsyncState)[2]);

				if (bytesReceived != bytesExpected)
					throw new Exception ("DIDNT RECIEVE ENOUGH BYTES!! GOT: " + bytesReceived + " EXPECTED: " + bytesExpected);

				byte[] buffer = (byte[]) ((object[])result.AsyncState)[0];
				buffer = base.Decrypt (buffer, 0, buffer.Length);
				ReceiveDataCallback callback = (ReceiveDataCallback) ((object[])result.AsyncState)[1];
				callback (this, bytesReceived);
			} catch (Exception ex) {
				Disconnect (ex);
			}
		}
		
		private void OnReceivedData (IAsyncResult result)
		{
			try {
				int bytesReceived = socket.EndReceive (result);

				byte[] buffer = (byte[]) ((object[])result.AsyncState)[0];
				int offset = Convert.ToInt32 (((object[])result.AsyncState)[1]);
				int size = Convert.ToInt32 (((object[])result.AsyncState)[2]);
				ReceiveDataCallback callback = (ReceiveDataCallback)  ((object[])result.AsyncState)[3];
				int totalReceived = Convert.ToInt32 (((object[])result.AsyncState)[4]);
				
				totalReceived += bytesReceived;

				//Console.WriteLine ("REC BUFFER: " + FileFind.Common.BytesToString (buffer));

				if (bytesReceived == 0) {
					Disconnect (null);
				} else if (totalReceived != size) {
					// Listen for more!!
					//LogManager.Current.WriteToLog ("DEBUG: Got {0} out of {1} bytes, listening for more...", totalReceived, size);
					BeginReceivePrivate (buffer, offset, size, callback, totalReceived);
				} else {
				//	LogManager.Current.WriteToLog ("DEBUG: {0} bytes!!", totalReceived);
				//	callback (this, bytesReceived);
					callback (this, totalReceived);
				}
			} catch (ObjectDisposedException) {
				Disconnect ();
			} catch (Exception ex) {
				Disconnect (ex);
			}
		}

		public override EndPoint RemoteEndPoint {
			get {
				return remoteEndPoint;
			}
		}
		
		public override string ToString () 
		{
			if (Incoming == true)
				return "UDP/INCOMING/" + remoteEndPoint.ToString ();
			else
				return "UDP/OUTGOING/" + remoteEndPoint.ToString ();
		}

		public override void Disconnect ()
		{
			Disconnect (null);
		}
		
		public override void Disconnect (Exception ex)
		{
			base.transportState = TransportState.Disconnected;

			if (ex != null)
				LogManager.Current.WriteToLog (ex.ToString());

			base.RaiseDisconnected (ex);
		}
	}
}
