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

			var typeBytes = new byte[2];
			Array.Copy (data, typeBytes, 2);
			MessageType = (MessageType) Utility.TwoBytesToInteger (typeBytes);
			
			var lengthBytes = new byte[2];
			Array.Copy (data, 2, lengthBytes, 0, 2);
			var length = Utility.TwoBytesToInteger (lengthBytes);

			var position = 20;
			
			while (position < length) {
				var attributeTypeBytes = new byte [2];
				Array.Copy (data, position, attributeTypeBytes, 0, 2);
				var attributeType = (MessageAttributeType) Utility.TwoBytesToInteger (attributeTypeBytes);
				position += 2;
				
				var attributeLengthBytes = new byte [2];
				Array.Copy (data, position, attributeLengthBytes, 0, 2);
				var attributeLength = Utility.TwoBytesToInteger (attributeLengthBytes);
				position += 2;

				var attributeData = new byte [attributeLength];
				Array.Copy (data, position, attributeData, 0, attributeLength);

				if (MessageAttribute.TypeTable.ContainsKey (attributeType)) {
					var type = MessageAttribute.TypeTable [attributeType];
					var attribute = (MessageAttribute) Activator.CreateInstance (type, attributeData);
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

			foreach (var attribute in MessageAttributes) {
				length += (ushort) attribute.Length;
			}

			var index = 0;
			var buffer = new byte [length];
	
			Array.Copy (Utility.IntegerToTwoBytes ((int)MessageType), 0, buffer, index, 2);
			index += 2;

			Array.Copy (Utility.IntegerToTwoBytes (length - 20), 0, buffer, index, 2);
			index += 2;

			Array.Copy (transactionID, 0, buffer, index, 16);
			index += 16;

			foreach (var attribute in MessageAttributes) {
				var attributeBytes = attribute.GetBytes ();
				Array.Copy (attributeBytes, 0, buffer, index, attributeBytes.Length);
				index += attributeBytes.Length;
			}
			
			return buffer;
		}
	
		private byte[] CreateTransactionID ()
		{
			var result = new byte [16];
			var random = new Random ();
		        random.NextBytes (result);
			return result;
		}

	}
}
