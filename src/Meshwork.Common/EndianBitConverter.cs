//
// EndianBitConverter.cs: An endian-safe BitConverter
//
// Author:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006 Meshwork Authors
// 

using System;

namespace Meshwork.Common
{
	public class EndianBitConverter
	{
		public static byte[] GetBytes (bool value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (char value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (double value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (short value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (int value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (long value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (float value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}


		public static byte[] GetBytes (ushort value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}	
		
		public static byte[] GetBytes (uint value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}	
		
		public static byte[] GetBytes (ulong value)
		{
			var result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}	
		
		public static int ToInt32 (byte[] value, int startIndex)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value, startIndex, 4);
			}
			return BitConverter.ToInt32(value, startIndex);
		}

		public static string ToString (byte[] value)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value);
			}
			return BitConverter.ToString(value);
		}
		
		public static string ToString (byte[] value, int startIndex,
		                               int length)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value);
			}
			return BitConverter.ToString(value, startIndex,
			                             length);
		}

		public static uint ToUInt32 (byte[] value, int startIndex)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value, startIndex, 4);
			}
			return BitConverter.ToUInt32(value, startIndex);
		}

		public static ulong ToUInt64 (byte[] value, int startIndex)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value, startIndex, 8);
			}
			return BitConverter.ToUInt64(value, startIndex);
		}
	}
}
