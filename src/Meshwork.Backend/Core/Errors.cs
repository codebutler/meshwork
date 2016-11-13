
using System;

namespace Meshwork.Backend.Core
{
	public class MeshworkError
	{
		string m_Message;

		public MeshworkError ()
		{
		}

		public MeshworkError (string Message)
		{
			m_Message = Message;
		}

		public MeshworkError (string Message, string[] Format)
		{
			m_Message = string.Format(Message, Format);
		}

		public virtual string Message {
			get { return m_Message; }
		}

		public override string ToString ()
		{
			return Message;
		}

		public Exception ToException ()
		{
			var e = new Exception(Message);
			return e;
		}
	}

	public abstract class FileTransferError : MeshworkError
	{
		string transferId;

		public FileTransferError ()
		{

		}

		public FileTransferError (string transferId)
		{
			this.transferId = transferId;
		}

		public string TransferId {
			get { return transferId; }
			set { transferId = value; }
		}
	}

	public class InvalidNetworkNameError : MeshworkError
	{
		string theirName;
		string ourName;

		public InvalidNetworkNameError ()
		{
		}

		public InvalidNetworkNameError (string theirName, string ourName)
		{
			this.theirName = theirName;
			this.ourName = ourName;
		}

		public override string Message {
			get { return "Connection was closed because remote node is configured with a different network name ('" + theirName + "' != '" + ourName + "')."; }
		}
	}

	public class VersionMismatchError : MeshworkError
	{
		string otherVersion = "";

		public VersionMismatchError ()
		{
		}

		public VersionMismatchError (string otherVersion)
		{
		}

		public override string Message {
			get
			{
			    //return "Connection was closed because remote node is using an incompatable version";
				if (otherVersion != null && otherVersion != "")
					return "Incompatable Version (" + otherVersion + ")";
			    return "Incompatable Version";
			}
		}
	}

	public class InvalidNicknameError : MeshworkError
	{
	    public override string Message {
			get { return "Connection was closed because remote node supplied an invalid nickname."; }
		}
	}

	public class FileTransferFirewallError : MeshworkError
	{
	    public override string Message {
			get { return "You cannot recieve files because you are behind a firewall."; }
		}
	}

	public class FileTransferNoTransfersError : MeshworkError
	{
	    public override string Message {
			get { return "You cannot recieve files because this node is configured not to allow incoming file transfer connections."; }
		}
	}

	public class DirectoryNotFoundError : MeshworkError
	{
		public string DirPath;

		public DirectoryNotFoundError ()
		{
		}

		public DirectoryNotFoundError (string notFoundDirPath)
		{
			DirPath = notFoundDirPath;
		}

		public override string Message {
			get { return "The specified directory does not exist (" + DirPath + ")."; }
		}
	}

	public class FileNotFoundError : FileTransferError
	{
		public string FilePath {
			get; set;
		}

		public FileNotFoundError ()
		{
		}

		public FileNotFoundError (string filePath)
		{
			FilePath = filePath;
		}

		public FileNotFoundError (string filePath, string transferId) : base(transferId)
		{
			FilePath = filePath;
		}

		public override string Message {
			get { return $"The specified file does not exist ({FilePath})."; }
		}
	}

	public class NotTrustedError : MeshworkError
	{
	    public override string Message {
			get { return "I ignored a message from you because you are not in my trusted nodes list."; }
		}
	}
}
