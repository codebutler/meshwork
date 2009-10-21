//
// Encryption.cs: Cryptography helper methods
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2005 FileFind.net (http://filefind.net)
//

using System;
using System.Text;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace FileFind.Meshwork.Security
{
	public static class Encryption 
	{
		public static byte[] Decrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0,
			                                     buffer.Length);
		}

		public static byte[] Encrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0,
			                                     buffer.Length);
		}

		public static string PasswordEncrypt (string password, string text)
		{
			//XXX: Shouldn't this be random data instead?
			byte[] salt = new byte[] { 0x00, 0x01, 0x02, 0x03,
			                           0x04, 0x05, 0x06, 0xF1,
						   0xF0, 0xEE, 0x21, 0x22,
						   0x45};

			Rfc2898DeriveBytes bytes;
			bytes = new Rfc2898DeriveBytes (password, salt);

			//XXX: This needs to be configurable
			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = bytes.GetBytes(16); // 16x16 = 256-bit key, right?
			alg.IV = bytes.GetBytes(16);

			using (MemoryStream ms = new MemoryStream()) {
				CryptoStream encryptStream;
				encryptStream = new CryptoStream(ms,
								 alg.CreateEncryptor(),
								 CryptoStreamMode.Write);
				
				byte[] buf = Encoding.UTF8.GetBytes(text);
				encryptStream.Write(buf, 0, buf.Length);
				encryptStream.Flush();
				encryptStream.Close();

				string retVal = Convert.ToBase64String(ms.ToArray());
				return retVal;
			}
		}

		public static string PasswordDecrypt(string password, string text)
		{
			//XXX: Shouldn't this be random data instead?
			byte[] salt = new byte[] { 0x00, 0x01, 0x02, 0x03,
			                           0x04, 0x05, 0x06, 0xF1,
						   0xF0, 0xEE, 0x21, 0x22,
						   0x45};

			Rfc2898DeriveBytes bytes;
			bytes = new Rfc2898DeriveBytes (password, salt);

			//XXX: This needs to be configurable
			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = bytes.GetBytes(16); // 16x16 = 256-bit key, right?
			alg.IV = bytes.GetBytes(16);

			using (MemoryStream ms = new MemoryStream()) {
				CryptoStream decryptStream = new CryptoStream(ms,
				                                              alg.CreateDecryptor(),
									      CryptoStreamMode.Write);
				byte[] buf = Convert.FromBase64String(text);
				decryptStream.Write(buf, 0, buf.Length);
				decryptStream.Close();
				string retVal = Encoding.UTF8.GetString(ms.ToArray());
				return retVal;
			}
		}
	}
}
