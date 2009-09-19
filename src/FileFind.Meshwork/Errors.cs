
using System;
using System.Xml.Serialization;
namespace FileFind.Meshwork.Errors
{
	[Serializable]
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
			m_Message = String.Format(Message, Format);
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
			Exception e = new Exception(this.Message);
			return e;
		}
	}

	[Serializable]
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

	[Serializable]
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

	[Serializable]
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
			get {
				//return "Connection was closed because remote node is using an incompatable version";
				if (otherVersion != null && otherVersion != "")
					return "Incompatable Version (" + otherVersion + ")";
				else
					return "Incompatable Version";
			}
		}
	}

	[Serializable]
	public class InvalidNicknameError : MeshworkError
	{

		public InvalidNicknameError ()
		{
		}

		public override string Message {
			get { return "Connection was closed because remote node supplied an invalid nickname."; }
		}
	}

	[Serializable]
	public class FileTransferFirewallError : MeshworkError
	{

		public FileTransferFirewallError ()
		{
		}

		public override string Message {
			get { return "You cannot recieve files because you are behind a firewall."; }
		}
	}

	[Serializable]
	public class FileTransferNoTransfersError : MeshworkError
	{

		public FileTransferNoTransfersError ()
		{
		}

		public override string Message {
			get { return "You cannot recieve files because this node is configured not to allow incoming file transfer connections."; }
		}
	}

	[Serializable]
	public class DirectoryNotFoundError : MeshworkError
	{
		public string DirPath;

		public DirectoryNotFoundError ()
		{
		}

		public DirectoryNotFoundError (string notFoundDirPath)
		{
			this.DirPath = notFoundDirPath;
		}

		public override string Message {
			get { return "The specified directory does not exist (" + DirPath + ")."; }
		}
	}

	[Serializable]
	public class FileNotFoundError : FileTransferError
	{
		string filePath;

		public string FilePath {
			get { return filePath; }
			set { filePath = value; }
		}

		public FileNotFoundError ()
		{
		}

		public FileNotFoundError (string filePath, string transferId) : base(transferId)
		{
			this.filePath = filePath;
		}

		public override string Message {
			get { return String.Format("The specified file does not exist ({0}).", filePath); }
		}
	}

	[Serializable]
	public class NotTrustedError : MeshworkError
	{

		public NotTrustedError ()
		{
		}

		public override string Message {
			get { return string.Format("I ignored a message from you because you are not in my trusted nodes list."); }
		}
	}
}
