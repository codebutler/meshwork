using System;
using System.Collections.Generic;

namespace Meshwork.Library.Stun
{
	public abstract class MessageAttribute
	{
		public static Dictionary <MessageAttributeType, Type> TypeTable;

		static MessageAttribute ()
		{
			TypeTable = new Dictionary <MessageAttributeType, Type> ();
			TypeTable.Add (MessageAttributeType.MappedAddress, typeof (MappedAddressAttribute));
			TypeTable.Add (MessageAttributeType.SourceAddress, typeof (SourceAddressAttribute));
		}


		public MessageAttribute (MessageAttributeType type)
		{
			this.type = type;
		}

		MessageAttributeType type;
		protected byte[] Value;

		public int Length {
			get {
				return Value.Length + 32;
			}
		}

		public byte[] GetBytes ()
		{
			byte[] buffer = new byte [Value.Length + 32];
		
			int index = 0;
			
			Array.Copy (BitConverter.GetBytes ((ushort)type), 0, buffer, index, 2);
			index += 2;

			Array.Copy (BitConverter.GetBytes ((ushort)Value.Length - 32), 0, buffer, index, 2);
			index += 2;
			
			Array.Copy (Value, 0, buffer, index, Value.Length);
			index += Value.Length;
			
			return buffer;
		}
	}

	public enum MessageAttributeType : ushort
	{
		MappedAddress = 0x0001,
		ResponseAddress = 0x0002,
		ChangeRequest = 0x0003,
		SourceAddress = 0x0004,
		ChangedAddress = 0x0005,
		Username = 0x0006,
		Password = 0x0007,
		MessageIntegrity = 0x0008,
		ErrorCode = 0x0009,
		UnknownAttributes = 0x00a,
		ReflectedFrom = 0x00b
	}


}
