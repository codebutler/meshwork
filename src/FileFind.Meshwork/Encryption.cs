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
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static byte[] Encrypt(ICryptoTransform transform, byte[] buffer)
		{
			return transform.TransformFinalBlock(buffer, 0, buffer.Length);
		}

		public static string PasswordEncrypt (string password, string text, byte[] salt)
		{
			Rfc2898DeriveBytes passwordBytes = new Rfc2898DeriveBytes(password, salt);
			
			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = passwordBytes.GetBytes(32);
			alg.IV = passwordBytes.GetBytes(16);
			
			byte[] buf = Encoding.UTF8.GetBytes(text);

			using (MemoryStream ms = new MemoryStream()) {
				CryptoStream encryptStream = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write);
				
				encryptStream.Write(buf, 0, buf.Length);
				encryptStream.Flush();
				encryptStream.Close();

				return Convert.ToBase64String(ms.ToArray());
			}
		}

		public static string PasswordDecrypt(string password, string text, byte[] salt)
		{
			Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, salt);

			SymmetricAlgorithm alg = RijndaelManaged.Create();
			alg.Key = bytes.GetBytes(32);
			alg.IV = bytes.GetBytes(16);

			byte[] buf = Convert.FromBase64String(text);

			using (MemoryStream ms = new MemoryStream()) {
				CryptoStream decryptStream = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write);
				
				decryptStream.Write(buf, 0, buf.Length);
				decryptStream.Close();
				
				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}
	}
}
