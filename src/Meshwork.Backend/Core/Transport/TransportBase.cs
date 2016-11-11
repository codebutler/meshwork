//
// TransportBase.cs: Handles stuff common to all transport implementations.
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Net;
using System.Runtime.Remoting.Messaging;
using Meshwork.Common;

namespace Meshwork.Backend.Core.Transport
{
	public abstract class TransportBase : ITransport
	{
		private delegate int SendReceiveCaller (byte[] buffer, int offset, int size);
		private delegate void MessageSendCaller (byte[] buffer);
		private delegate byte[] MessageReceiveCaller ();
			
		IMeshworkOperation  operation;
		Network             network;
		ITransportEncryptor encryptor;

		protected ulong connectionType;
		protected bool incoming;
		protected TransportState transportState;

		public abstract int Send (byte[] buffer, int offset, int size);
		public abstract int Receive (byte[] buffer, int offset, int size);
				
		public abstract void Connect (TransportCallback callback);

		public abstract void Disconnect ();
		public abstract void Disconnect (Exception ex);
		
		public event TransportEventHandler Connected;
		public event TransportErrorEventHandler Disconnected;
		
		public IMeshworkOperation Operation  {
			get {
				return operation;
			}
			set {
				operation = value;
			}
		}

		public abstract EndPoint RemoteEndPoint {
			get;
		}

		public ulong ConnectionType {
			set {
				connectionType = value;
			}
			get {
				return connectionType;
			}
		}

		public bool Incoming {
			get {
				return incoming;
			}
		}

		public TransportState State {
			get {
				if (transportState == TransportState.Connected && encryptor != null && encryptor.Ready == false) {
					return TransportState.Securing;
				} else {
					return transportState;
				}
			}
		}
		
		public ITransportEncryptor Encryptor {
			get {
				return encryptor;
			}
			set {
				encryptor = value;
			}
		}
		
		protected void RaiseConnected ()
		{
			if (Connected != null)
				Connected(this);
		}

		protected void RaiseDisconnected (Exception ex)
		{
			if (Disconnected != null) {
				Disconnected ((ITransport)this, ex);
			}
		}

		public Network Network {
			get {
				return network;
			}
			set {
				network = value;
			}
		}

		public int Send (byte[] buffer)
		{
			return Send(buffer, 0, buffer.Length);
		}

		public int Receive (byte[] buffer)
		{
			return Receive(buffer, 0, buffer.Length);
		}

		public void SendMessage (byte[] buffer)
		{
			if (buffer == null) {
				throw new ArgumentNullException("buffer");
			}

			if (encryptor != null) {
				buffer = encryptor.Encrypt(buffer);
			}

			byte[] dataSizeBytes = EndianBitConverter.GetBytes(buffer.Length);
			byte[] realBuffer = new byte[buffer.Length + dataSizeBytes.Length];
			Array.Copy (dataSizeBytes, 0, realBuffer, 0, dataSizeBytes.Length);
			Array.Copy (buffer, 0, realBuffer, dataSizeBytes.Length, buffer.Length);

			Send(realBuffer);
		}

		public IAsyncResult BeginSendMessage (byte[] buffer, AsyncCallback callback, object state)
		{
			if (buffer == null) {
				throw new ArgumentNullException("buffer");
			} else if (callback == null) {
				throw new ArgumentNullException("callback");
			}
			
			MessageSendCaller caller = new MessageSendCaller(SendMessage);
			return caller.BeginInvoke(buffer, callback, state);
		}
		
		object foo = new object();

		public byte[] ReceiveMessage()
		{
			try {
				lock (foo) {
					// get the message size 
					byte[] messageSizeBytes = new byte[4];
					int    dataLength;
	
					int count = Receive(messageSizeBytes, 0, 4);
	
					if (count != 4) {
						throw new Exception(string.Format("Received wrong amount in message size! Got: {0}, Expected: {1}", count, 4));
					}
	
					dataLength = EndianBitConverter.ToInt32(messageSizeBytes, 0);
	
					// get the message
					byte[] messageBytes = new byte[dataLength];
					
					count = Receive(messageBytes, 0, dataLength);
	
					if (count != dataLength) {
						throw new Exception(string.Format("Received wrong amount! Got: {0}, Expected: {1}", count, dataLength));
					}
					
					if (encryptor != null) {
						messageBytes = encryptor.Decrypt(messageBytes);
					}
					
					return messageBytes;
				}
			} catch (Exception ex) {
				Disconnect(ex);
				return null;
			}
		}

		public IAsyncResult BeginReceive (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			SendReceiveCaller caller = new SendReceiveCaller(Receive);
			return caller.BeginInvoke(buffer, offset, size, callback, state);
		}

		public int EndReceive (IAsyncResult asyncResult)
		{
			return ((SendReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
		
		public IAsyncResult BeginSend (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			SendReceiveCaller caller = new SendReceiveCaller(Send);
			return caller.BeginInvoke(buffer, offset, size, callback, state);
		}
		
		public int EndSend (IAsyncResult asyncResult)
		{
			return ((SendReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}

		public IAsyncResult BeginReceiveMessage(AsyncCallback callback, object state)
		{
			MessageReceiveCaller caller = new MessageReceiveCaller(ReceiveMessage);
			return caller.BeginInvoke(callback, state);
		}
		
		public void EndSendMessage (IAsyncResult asyncResult)
		{
			((MessageSendCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
		
		public byte[] EndReceiveMessage (IAsyncResult asyncResult)
		{
			return ((MessageReceiveCaller)((AsyncResult)asyncResult).AsyncDelegate).EndInvoke(asyncResult);
		}
	}
}
