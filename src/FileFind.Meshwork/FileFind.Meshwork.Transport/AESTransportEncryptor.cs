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

namespace FileFind.Meshwork.Transport
{
	public class AESTransportEncryptor : ITransportEncryptor
	{
		//TODO: This should be configurable.
		int keySize = 16;
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
			this.algorithm = algorithm;
			this.keyBytes = keyBytes;
			this.ivBytes = ivBytes;
			
			algorithm = new RijndaelManaged();
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
