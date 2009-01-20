//
// EndianBitConverter.cs: An endian-safe BitConverter
//
// Author:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net/)
// 

using System;

namespace FileFind.Meshwork
{
	public class EndianBitConverter
	{
		public static byte[] GetBytes (bool value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (char value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (double value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (short value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (int value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (long value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}

		public static byte[] GetBytes (float value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}


		public static byte[] GetBytes (ushort value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}	
		
		public static byte[] GetBytes (uint value)
		{
			byte[] result = BitConverter.GetBytes(value);
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(result);
			}
			return result;
		}	
		
		public static byte[] GetBytes (ulong value)
		{
			byte[] result = BitConverter.GetBytes(value);
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

		public static UInt32 ToUInt32 (byte[] value, int startIndex)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value, startIndex, 4);
			}
			return BitConverter.ToUInt32(value, startIndex);
		}

		public static UInt64 ToUInt64 (byte[] value, int startIndex)
		{
			if (!BitConverter.IsLittleEndian) {
				Array.Reverse(value, startIndex, 8);
			}
			return BitConverter.ToUInt64(value, startIndex);
		}
	}
}
