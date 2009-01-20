//
// Exceptions.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006 FileFind.net (http://filefind.net)
//

using System;
using System.Xml.Serialization;
namespace FileFind.Meshwork.Exceptions
{	
	[Serializable]
	public abstract class FileTransferException : MeshworkException
	{
		string transferId;

		public FileTransferException ()
		{

		}

		public FileTransferException (string transferId)
		{
			this.transferId = transferId;
		}

		public string TransferId {
			get {
				return transferId;
			}
			set {
				transferId = value;
			}
		}
	}

	[Serializable]
	public class InvalidNetworkNameException : MeshworkException {
		string theirName;
		string ourName;

		public InvalidNetworkNameException ()
		{
		}

		public InvalidNetworkNameException (string theirName, string ourName)
		{
			this.theirName = theirName;
			this.ourName = ourName;
		}
 
		public override string Message {
			get {
				return "Connection was closed because remote node is configured with a different network name ('" + theirName + "' != '" + ourName + "').";
			}
		}
	}

	[Serializable]
	public class VersionMismatchException : MeshworkException
	{
		string otherVersion = "";

		public VersionMismatchException()
		{
		}

		public VersionMismatchException (string otherVersion)
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
	public class InvalidNicknameException : MeshworkException
	{

		public InvalidNicknameException()
		{
		}

		public override string Message {
			get {
				return "Connection was closed because remote node supplied an invalid nickname.";
			}
		}
	}

	[Serializable]
	public class FileTransferFirewallException : MeshworkException
	{

		public FileTransferFirewallException()
		{
		}

		public override string Message {
			get {
				return "You cannot recieve files because you are behind a firewall.";
			}
		}
	}

	[Serializable()] public class FileTransferNoTransfersException : MeshworkException {

		public FileTransferNoTransfersException() {
		}

		public override string Message {
			get {
				return "You cannot recieve files because this node is configured not to allow incoming file transfer connections.";
			}
		}
	}

	[Serializable()]
	public class DirectoryNotFoundException : MeshworkException
	{
		public string DirPath;

		public DirectoryNotFoundException() {
		}

		public DirectoryNotFoundException(string notFoundDirPath) {
			this.DirPath = notFoundDirPath;
		}

		public override string Message {
			get {
				return "The specified directory does not exist (" + DirPath + ").";
			}
		}
	}

	[Serializable]
	public class FileNotFoundException : FileTransferException
	{
		string filePath;

		public string FilePath {
			get {
				return filePath;
			}
			set {
				filePath = value;
			}
		}

		public FileNotFoundException()
		{
		}

		public FileNotFoundException(string filePath, string transferId) : base(transferId)
		{
			this.filePath = filePath;
		}

		public override string Message {
			get {
				return String.Format("The specified file does not exist ({0}).", filePath);
			}
		}
	}

	[Serializable()] public class FileAlreadyDownloadedException : Exception {

		public FileAlreadyDownloadedException() {
		}

		public override string Message {
			get {
				return "You have already finished downloading the selected file.";
			}
		}
	}
	[Serializable()] public class MeshworkException {
		string _Message;

		public MeshworkException() {
		}

		public MeshworkException(string Message) {
			_Message = Message;
		}

		public MeshworkException(string Message, string[] Format) {
			_Message = string.Format(Message, Format);
		}

		public virtual string Message {
			get {
				return _Message;
			}
		}

		public override string ToString() {
			return Message;
		}

		public Exception ToException() {
			Exception e = new Exception(this.Message);
			return e;
		}
	}
	[Serializable()] public class KeyNotAvaliableException : Exception {
		Node _node;
		string _messageType;

		public KeyNotAvaliableException(Node N, MessageType messageType) {
			_node = N;
			_messageType = messageType.ToString();
		}

		public KeyNotAvaliableException() {
		}

		public override string Message {
			get {
				return string.Format("Unable to send {1} message to {0} because a private key has not been generated for this node!", _node.ToString(), _messageType);
			}
		}
	}
	[Serializable()] public class UnableToDecryptException : Exception {

		public UnableToDecryptException() {
		}

		public override string Message {
			get {
				return string.Format("Unable to decrypt message contents!");
			}
		}
	}
	[Serializable()] public class InvalidSignatureException : Exception {

		public InvalidSignatureException() {
		}

		public override string Message {
			get {
				return string.Format("Message had an invalid signature!");
			}
		}
	}
	[Serializable()] public class NotTrustedException : MeshworkException {

		public NotTrustedException() {
		}

		public override string Message {
			get {
				return string.Format("I ignored a message from you because you are not in my trusted nodes list.");
			}
		}
	}
	public class PasswordIncorrectException : Exception {

		public override string Message {
			get {
				return "Password Incorrect";
			}
		}
	}
	public class UnableToConnectException : Exception {

		public override string Message {
			get {
				return string.Format("Unable to connect to the remote host");
			}
		}
	}
	public class AlreadyConnectedException : Exception {
		string _IP;

		public AlreadyConnectedException(string IP) {
			_IP = IP;
		}

		public override string Message {
			get {
				return string.Format("Connection to was closed because a connection to " + _IP + " already exists.");
			}
		}
	}
	public class ConnectNotTrustedException : Exception {
		//private string _nodeid;

		public ConnectNotTrustedException () {
		//public ConnectNotTrustedException(string nodeid) {
		//	_nodeid = nodeid;
		}

		public override string Message {
			get {
				return "Not Trusted";
				//return string.Format("Connection to was closed because remote node is not in trusted node list (NodeID: {0}).", _nodeid);
			}
		}
	}
	public class ConnectNotAllowedException : Exception {
		private string _nodeid;

		public ConnectNotAllowedException(string nodeid) {
			_nodeid = nodeid;
		}

		public override string Message {
			get {
				return string.Format("Connection to was closed because you have selected to not allow connections with this node (NodeID: {0}).", _nodeid);
			}
		}
	}
	public class ConnectToSelfException : Exception {

		public override string Message {
			get {
				return string.Format("Connection was closed because you tried to connect to yourself! Naughty boy!");
			}
		}
	}
	public class ConnectionTimeoutException : Exception {
		string _Host;

		public ConnectionTimeoutException(string host) {
			_Host = host;
		}

		public override string Message {
			get {
				return "Unable to connect to " + _Host + ": Connection timed out.";
			}
		}
	}
	public class ConnectionFailedException : Exception {

		public override string Message {
			get {
				return "No connection could be made because the target machine actively refused it";
			}
		}
	}
}
