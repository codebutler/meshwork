//
// Serialization.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System.Xml;
using System.Xml.Serialization;
using System.Text;
using System.IO;
using System;
using System.Security;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace FileFind.Serialization
{
	public class Xml
	{
		public static string Serialize(object Obj) {
			StringBuilder sb = new StringBuilder();
			StringWriterWithEncoding HappyStringWriter = new StringWriterWithEncoding(sb, Encoding.UTF8);
			XmlSerializer x = new XmlSerializer(Obj.GetType());
			x.Serialize(HappyStringWriter, Obj);
			return HappyStringWriter.ToString();
		}	
		
		public static object DeSerialize(string Str, Type ObjectType)
		{
			byte[] b = System.Text.Encoding.UTF8.GetBytes(Str);
			using (MemoryStream ms = new MemoryStream()) {
				ms.Write(b, 0, b.Length);
				ms.Position = 0;
				XmlSerializer x = new XmlSerializer(ObjectType);
				return x.Deserialize(ms);
			}
		}
	}

	public class Binary
	{
		private static BinaryFormatter formatter = new BinaryFormatter();

		public static byte[] Serialize(object obj)
		{
			using (MemoryStream ms = new MemoryStream()) {
				formatter.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		public static object Deserialize(byte[] content)
		{
			using (MemoryStream ms = new MemoryStream(content)) {
				return formatter.Deserialize(ms);
			}
		}
	}

	// This is based on code from
	// http://groups.google.com/group/microsoft.public.dotnet.languages.csharp/msg/7e43c0f0613adce1
	public class Raw
	{
		public static byte[] Serialize (object obj)
		{
			int rawsize = Marshal.SizeOf (obj);
			byte[] rawdata = new byte[rawsize];
			GCHandle handle = GCHandle.Alloc (rawdata, GCHandleType.Pinned);
			IntPtr buffer = handle.AddrOfPinnedObject();
			Marshal.StructureToPtr (obj, buffer, false);
			handle.Free ();
			return rawdata;
		}

		public static object Deserialize (byte[] rawdata, Type type)
		{
			int rawsize = Marshal.SizeOf (type);
			if (rawsize > rawdata.Length) {
				throw new Exception ("Something went wrong here.");
			}
			GCHandle handle = GCHandle.Alloc (rawdata, GCHandleType.Pinned);
			IntPtr buffer = handle.AddrOfPinnedObject();
			object retobj = Marshal.PtrToStructure (buffer, type);
			handle.Free ();
			return retobj;
		}
	}
}
