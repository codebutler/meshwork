//
// Encryption.cs: Cryptography helper methods
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2005 Meshwork Authors
//

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Meshwork.Backend.Core
{
	public static class Encryption 
	{
		public static byte[] Decrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static byte[] Encrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static string PasswordEncrypt (string password, string text, byte[] salt)
		{
			var passwordBytes = new Rfc2898DeriveBytes(password, salt);
			
			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = passwordBytes.GetBytes(32);
			alg.IV = passwordBytes.GetBytes(16);
			
			var buf = Encoding.UTF8.GetBytes(text);

			using (var ms = new MemoryStream()) {
				var encryptStream = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
				
				encryptStream.Write(buf, 0, buf.Length);
				encryptStream.Flush();
				encryptStream.Close();

				return Convert.ToBase64String(ms.ToArray());
			}
		}

		public static string PasswordDecrypt(string password, string text, byte[] salt)
		{
			var bytes = new Rfc2898DeriveBytes(password, salt);

			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = bytes.GetBytes(32);
			alg.IV = bytes.GetBytes(16);

			var buf = Convert.FromBase64String(text);

			using (var ms = new MemoryStream()) {
				var decryptStream = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
				
				decryptStream.Write(buf, 0, buf.Length);
				decryptStream.Close();
				
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}
	}
}
