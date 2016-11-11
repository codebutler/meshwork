//
// EventArgs.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using Meshwork.Backend.Core.Protocol;

namespace Meshwork.Backend.Core
{
	/*
	public class FileOfferedEventArgs
	{
		Node from;
		SharedFileInfo file;

		public FileOfferedEventArgs (Node from, SharedFileInfo file)
		{
			this.from = from;
			this.file = file;
		}

		public Node From {
			get {
				return from;
			}
		}

		public SharedFileInfo File {
			get {
				return file;
			}
		}
	}
	*/

	public class ReceivedKeyEventArgs
	{
		Node node;
		LocalNodeConnection connection;
		
		KeyInfo keyInfo;
		
		public ReceivedKeyEventArgs (Node node, KeyInfo keyInfo)
		{
			this.node = node;
			this.keyInfo = keyInfo;
		}
		
		public ReceivedKeyEventArgs (LocalNodeConnection connection, KeyInfo keyInfo)
		{
			this.connection = connection;
			this.keyInfo = keyInfo;
		}		

		public Node Node {
			get {
				return node;
			}
		}
		
		public LocalNodeConnection Connection {
			get {
				return connection;
			}
		}

		public KeyInfo Key {
			get {
				return keyInfo;
			}
		}
	}

	public class ChatEventArgs
	{
		Node node;
		ChatRoom room;

		public ChatEventArgs (Node node, ChatRoom room)
		{
			this.node = node;
			this.room = room;
		}

		public Node Node {
			get {
				return node;
			}
		}

		public ChatRoom Room {
			get {
				return room;
			}
		}
	}

	public class SearchResultInfoEventArgs
	{
		Node node;
		SearchResultInfo info;

		public SearchResultInfoEventArgs (Node node, SearchResultInfo info)
		{
			this.node = node;
			this.info = info;
		}

		public Node Node {
			get {
				return node;
			}
		}

		public SearchResultInfo Info {
			get {
				return info;
			}
		}
	}

}
