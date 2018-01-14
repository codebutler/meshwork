//
// ITransport.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System;
using System.Net;

namespace Meshwork.Backend.Core.Transport
{
	public delegate void TransportCallback (ITransport transport);
	public delegate void TransportErrorEventHandler (ITransport transport, Exception ex);

	public interface ITransport
	{
		int Send (byte[] buffer);
		int Send (byte[] buffer, int offset, int size);
		int Receive (byte[] buffer);
		int Receive (byte[] buffer, int offset, int size);

		IAsyncResult BeginSend (byte[] buffer, int offset, int size, AsyncCallback callback, object state);
		IAsyncResult BeginReceive (byte[] buffer, int offset, int size, AsyncCallback callback, object state);

		int EndSend (IAsyncResult asyncResult);
		int EndReceive (IAsyncResult asyncResult);
		
		void SendMessage (byte[] buffer);
		byte[] ReceiveMessage();

		IAsyncResult BeginSendMessage (byte[] buffer, AsyncCallback callback, object state);
		IAsyncResult BeginReceiveMessage (AsyncCallback callback, object state);
		
		void EndSendMessage (IAsyncResult asyncResult);
		byte[] EndReceiveMessage (IAsyncResult asyncResult);		
		
		void Connect (TransportCallback callback);

		void Disconnect ();
		void Disconnect (Exception ex);

		event TransportEventHandler Connected;
		event TransportErrorEventHandler Disconnected;

		ITransportEncryptor Encryptor {
			get;
			set; // XXX: Get rid of this setter
		}
		
		Network Network {
			get;
			set;
		}

		ulong ConnectionType {
			get;
			set;
		}

		bool Incoming {
			get;
		}

		EndPoint RemoteEndPoint {
			get;
		}
		
		TransportState State {
			get;
		}

		IMeshworkOperation Operation {
			get;
			set;
		}
	}
}
