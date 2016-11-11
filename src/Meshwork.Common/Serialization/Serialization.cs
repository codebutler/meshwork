//
// Serialization.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Meshwork.Common.Serialization
{
	public static class Xml
	{
		public static string Serialize(object obj) {
			var sb = new StringBuilder();
			var writer = new StringWriterWithEncoding(sb, Encoding.UTF8);
			var x = new XmlSerializer(obj.GetType());
			x.Serialize(writer, obj);
			return writer.ToString();
		}

		public static object DeSerialize(string str, Type objectType)
		{
			var b = System.Text.Encoding.UTF8.GetBytes(str);
			using (var ms = new MemoryStream()) {
				ms.Write(b, 0, b.Length);
				ms.Position = 0;
				var x = new XmlSerializer(objectType);
				return x.Deserialize(ms);
			}
		}
	}

	public static class Binary
	{
		private static readonly BinaryFormatter formatter = new BinaryFormatter();

		public static byte[] Serialize(object obj)
		{
			using (var ms = new MemoryStream()) {
				formatter.Serialize(ms, obj);
				return ms.ToArray();
			}
		}

		public static object Deserialize(byte[] content)
		{
			using (var ms = new MemoryStream(content)) {
				return formatter.Deserialize(ms);
			}
		}
	}

	// This is based on code from
	// http://groups.google.com/group/microsoft.public.dotnet.languages.csharp/msg/7e43c0f0613adce1
	public static class Raw
	{
		public static byte[] Serialize (object obj)
		{
			var rawsize = Marshal.SizeOf (obj);
			var rawdata = new byte[rawsize];
			var handle = GCHandle.Alloc (rawdata, GCHandleType.Pinned);
			var buffer = handle.AddrOfPinnedObject();
			Marshal.StructureToPtr (obj, buffer, false);
			handle.Free ();
			return rawdata;
		}

		public static object Deserialize (byte[] rawdata, Type type)
		{
			var rawsize = Marshal.SizeOf (type);
			if (rawsize > rawdata.Length) {
				throw new Exception ("Something went wrong here.");
			}
			var handle = GCHandle.Alloc (rawdata, GCHandleType.Pinned);
			var buffer = handle.AddrOfPinnedObject();
			var retobj = Marshal.PtrToStructure (buffer, type);
			handle.Free ();
			return retobj;
		}
	}

	public static class JSON
	{
		public static string Serialize (object obj)
		{
			// var serializer = new JavaScriptSerializer();
			// return serializer.Serialize(obj);
			return JsonConvert.SerializeObject(obj);
		}

		public static object Deserialize (string json)
		{
			return JsonConvert.DeserializeObject(json);
		}
	}
}
