//
// TransportManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using Meshwork.Common;
using Org.Mentalis.Security.Cryptography;

namespace Meshwork.Backend.Core.Transport
{
	public delegate void TransportEventHandler (ITransport transport);

	public class TransportManager
	{
		public event TransportEventHandler NewTransportAdded;
		public event TransportEventHandler TransportRemoved;
		public event TransportErrorEventHandler TransportError;

		List<ITransport> transports = new List<ITransport>();
		Dictionary<Type, string> friendlyNames = new Dictionary<Type, string>();

	    private readonly Core core;

		public TransportManager (Core core)
		{
		    this.core = core;
			friendlyNames.Add(typeof(TcpTransport), "TCP");
		}

		public string GetFriendlyName (Type type)
		{
			return friendlyNames[type];
		}

		public ITransport[] Transports {
			get {
				return transports.ToArray();
			}
		}

		public int TransportCount {
			get {
				return transports.Count;
			}
		}

		Dictionary<ITransport, TransportCallback> connectCallbacks = new Dictionary<ITransport, TransportCallback> ();

		internal void Add (ITransport transport)
		{
			Add (transport, null);
		}

		internal void Add (ITransport transport, TransportCallback connectCallback)
		{
			try {
				// XXX: This should be negotiated as part of the initial handshake.
				transport.Encryptor = new AESTransportEncryptor();

				transports.Add (transport);

				if (NewTransportAdded != null)
					NewTransportAdded (transport);

				LoggingService.LogInfo($"Transport {transport} added");

				if (transport.Incoming) {
					if (connectCallback != null)
						throw new ArgumentException ("You can only specify a ConnectCallback for outoging connections!");

					if (transport.Encryptor != null) {
						var dh = new DiffieHellmanManaged ();

						var keyxBytes = new byte[transport.Encryptor.KeyExchangeLength];
						transport.Receive (keyxBytes, 0, keyxBytes.Length);
						keyxBytes = dh.DecryptKeyExchange (keyxBytes);

						var keyBytes = new byte[transport.Encryptor.KeySize];
						var ivBytes = new byte[transport.Encryptor.IvSize];
						Array.Copy (keyxBytes, 0, keyBytes, 0, keyBytes.Length);
						Array.Copy (keyxBytes, keyBytes.Length, ivBytes, 0, ivBytes.Length);

						keyxBytes = dh.CreateKeyExchange ();
						transport.Send (keyxBytes, 0, keyxBytes.Length);

						transport.Encryptor.SetKey(keyBytes, ivBytes);
					}

					//Receive connection type, which is a ulong (8 bytes)
					var responseBuffer = new byte[8];
				    	transport.Receive (responseBuffer, 0, 8);
					var connectionType = EndianBitConverter.ToUInt64 (responseBuffer, 0);

					// Recieve network ID (64 bytes)
					responseBuffer = new byte[64];
					transport.Receive (responseBuffer, 0, 64);
					var networkId = EndianBitConverter.ToString (responseBuffer).Replace ("-", "");

					// Match to one of our known networks!
					foreach (var network in core.Networks) {
						if (network.NetworkID == networkId) {
							transport.Network = network;
						}
					}

					if (transport.Network == null) {
						throw new Exception ($"Unknown network: {networkId}.");
					}

					transport.ConnectionType = connectionType;

					if (connectionType == ConnectionType.NodeConnection) {
						var connection = new LocalNodeConnection(transport);
						transport.Operation = connection;
						transport.Network.AddConnection(connection);
						connection.Start();
					} else if (connectionType == ConnectionType.TransferConnection) {

						core.FileTransferManager.NewIncomingConnection(transport);

					} else {
						throw new Exception($"Unknown connection type: {connectionType}.");
					}

				} else {
					if (connectCallback == null) {
						throw new ArgumentNullException(nameof(connectCallback));
					}

					connectCallbacks.Add (transport, connectCallback);

					LoggingService.LogInfo("Transport {0} connecting...", transport);

					TransportCallback callback = OnConnected;
					transport.Connect (callback);
				}
			} catch (Exception ex) {
				transport.Disconnect (ex);
				RaiseTransportError(transport, ex);
			}
		}

		public void Remove (ITransport transport)
		{
			// XXX: Do anything else here before removing?
			transport.Disconnect();

			transports.Remove(transport);

			if (TransportRemoved != null)
				TransportRemoved(transport);

			LoggingService.LogInfo("Transport {0} removed", transport);
		}

		private void OnConnected (ITransport transport)
		{
			try {
				LoggingService.LogInfo("Transport {0} connected.", transport);

				if (transport.Encryptor != null) {
					var dh = new DiffieHellmanManaged ();

					var keyxBytes = dh.CreateKeyExchange ();
					transport.Send (dh.CreateKeyExchange (), 0, keyxBytes.Length);

					keyxBytes = new byte [transport.Encryptor.KeyExchangeLength];
					transport.Receive (keyxBytes, 0, transport.Encryptor.KeyExchangeLength);

					keyxBytes = dh.DecryptKeyExchange (keyxBytes);

					var keyBytes = new byte[transport.Encryptor.KeySize];
					var ivBytes = new byte[transport.Encryptor.IvSize];
					Array.Copy (keyxBytes, 0, keyBytes, 0, keyBytes.Length);
					Array.Copy (keyxBytes, keyBytes.Length, ivBytes, 0, ivBytes.Length);

					transport.Encryptor.SetKey(keyBytes, ivBytes);
				}

				var connectionType = EndianBitConverter.GetBytes (transport.ConnectionType);
				transport.Send (connectionType, 0, connectionType.Length);

				var networkId = Common.Utils.SHA512 (transport.Network.NetworkName);
				transport.Send (networkId, 0, networkId.Length);

				// Ready, Steady, GO!

				var callback = connectCallbacks [transport];
				connectCallbacks.Remove (transport);
				callback (transport);

			} catch (Exception ex) {
				transport.Disconnect (ex);
				RaiseTransportError(transport, ex);
			}
		}

		private void RaiseTransportError(ITransport transport, Exception ex)
		{
			if (ex != null) {
				LoggingService.LogError("Transport disconnected with error.", ex);
			} else {
				LoggingService.LogError("Transport disconnected.");
			}

			if (TransportError != null) {
				TransportError (transport, ex);
			}
		}
	}
}
