using System;
using System.Collections.Generic;

namespace Meshwork.Library.Stun
{
	public class MessageHeader
	{
		byte [] transactionID;
		public MessageHeader ()
		{
			MessageAttributes = new List <MessageAttribute> ();
			transactionID = CreateTransactionID ();
		}

		public MessageHeader (byte[] data)
		{
			MessageAttributes = new List <MessageAttribute> ();

			byte[] typeBytes = new byte[2];
			Array.Copy (data, typeBytes, 2);
			MessageType = (MessageType) Utility.TwoBytesToInteger (typeBytes);
			
			byte[] lengthBytes = new byte[2];
			Array.Copy (data, 2, lengthBytes, 0, 2);
			int length = Utility.TwoBytesToInteger (lengthBytes);

			int position = 20;
			
			while (position < length) {
				byte[] attributeTypeBytes = new byte [2];
				Array.Copy (data, position, attributeTypeBytes, 0, 2);
				MessageAttributeType attributeType = (MessageAttributeType) Utility.TwoBytesToInteger (attributeTypeBytes);
				position += 2;
				
				byte[] attributeLengthBytes = new byte [2];
				Array.Copy (data, position, attributeLengthBytes, 0, 2);
				int attributeLength = Utility.TwoBytesToInteger (attributeLengthBytes);
				position += 2;

				byte[] attributeData = new byte [attributeLength];
				Array.Copy (data, position, attributeData, 0, attributeLength);

				if (MessageAttribute.TypeTable.ContainsKey (attributeType) == true) {
					Type type = MessageAttribute.TypeTable [attributeType];
					MessageAttribute attribute = (MessageAttribute) Activator.CreateInstance (type, new object[] {attributeData});
					MessageAttributes.Add (attribute);
				}

				
				position += attributeLength;
			}
			
		}

		public MessageType MessageType;
		
		public byte[] TransactionID {
			get {
				return transactionID;
			}
		}
		
		public List <MessageAttribute> MessageAttributes;
		
		public byte[] GetBytes ()
		{
			ushort length = 20;

			foreach (MessageAttribute attribute in MessageAttributes) {
				length += (ushort) attribute.Length;
			}

			int index = 0;
			byte [] buffer = new byte [length];
	
			Array.Copy (Utility.IntegerToTwoBytes ((int)MessageType), 0, buffer, index, 2);
			index += 2;

			Array.Copy (Utility.IntegerToTwoBytes (length - 20), 0, buffer, index, 2);
			index += 2;

			Array.Copy (transactionID, 0, buffer, index, 16);
			index += 16;

			foreach (MessageAttribute attribute in MessageAttributes) {
				byte [] attributeBytes = attribute.GetBytes ();
				Array.Copy (attributeBytes, 0, buffer, index, attributeBytes.Length);
				index += attributeBytes.Length;
			}
			
			return buffer;
		}
	
		private byte[] CreateTransactionID ()
		{
			byte[] result = new byte [16];
			Random random = new Random ();
		        random.NextBytes (result);
			return result;
		}

	}
}
