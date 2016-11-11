//
// AESTransportEncryptor.cs: Encrypt transport data using AES
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Security.Cryptography;

namespace Meshwork.Backend.Core.Transport
{
	public class AESTransportEncryptor : ITransportEncryptor
	{
		int keySize = 32;
		int ivSize = 16;
		
		RijndaelManaged algorithm;

		byte[] keyBytes;
		byte[] ivBytes;
		
		public AESTransportEncryptor()
		{

		}

		public int KeySize {
			get {
				return keySize;
			}
		}
		
		public int IvSize {
			get {
				return ivSize;
			}
		}
		
		public int KeyExchangeLength {
			get {
				// XXX: return keySize + ivSize;
				return 128;
			}
		}
		
		public void SetKey  (byte[] keyBytes, byte[] ivBytes)
		{
			this.keyBytes = keyBytes;
			this.ivBytes = ivBytes;
			
			this.algorithm = new RijndaelManaged();
		}
		
		public byte[] Encrypt (byte[] buffer)
		{
			if (algorithm != null) {
				ICryptoTransform encryptor = algorithm.CreateEncryptor(keyBytes, ivBytes);
				return encryptor.TransformFinalBlock(buffer, 0, buffer.Length);
			} else {
				throw new Exception("No key");
			}
		}
		
		public byte[] Decrypt (byte[] buffer)
		{
			if (algorithm != null) {
				ICryptoTransform decryptor = algorithm.CreateDecryptor(keyBytes, ivBytes);
				return decryptor.TransformFinalBlock(buffer, 0, buffer.Length);
			} else {
				throw new Exception("No key");
			}
		}

		public bool Ready {
			get {
				return (algorithm != null);
			}
		}
	}
}
