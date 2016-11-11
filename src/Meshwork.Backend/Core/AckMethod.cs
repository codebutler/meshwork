//
// AckMethod.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;

namespace Meshwork.Backend.Core
{
	/// <summary>
	/// Stores information used to invoke a method after recieving 
	/// an ack for a specific messageID.
	/// </summary>
	public class AckMethod
	{
		public AckMethod ()
		{
		}

		public AckMethod (string messageID, MethodEventHandler method, object[] args)
		{
			this.MessageID = messageID;
			this.Method = method;
			this.args = args;
		}

		/// <summary> The MessageID this <see cref="AckMethod"/> is intended for. </summary>
		public string MessageID;

		/// <summary> The EventHandler used to invoke the method </summary>
		public delegate void MethodEventHandler(DateTime timeReceived, object[] args);

        	/// <summary> The method to invoke </summary>
		public event MethodEventHandler Method;

		/// <summary> An object[] of arguments to be passed to Method </summary>
		public object[] args;

		/// <summary>
		/// Invokes the method.
		/// </summary>
		/// <param name="TimeReceived">The DateTime the ack for this message was received.</param>
		public void CallMethod(DateTime TimeReceived)
		{
			if (Method != null) {
				Method(TimeReceived, args);
			}
		}
	}
}
