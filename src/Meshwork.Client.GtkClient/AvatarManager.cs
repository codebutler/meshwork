//
// AvatarManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2006-2008 FileFind.net (http://filefind.net)
//

using System;
using System.IO;
using System.Collections.Generic;
using Gdk;
using Meshwork.Backend.Core;

namespace Meshwork.Client.GtkClient
{
	public class AvatarManager : IAvatarManager
	{
	    private readonly Core core;

		Dictionary<string, Pixbuf> avatars = new Dictionary<string, Pixbuf>();
		Dictionary<string, Pixbuf> smallAvatars = new Dictionary<string, Pixbuf>();
		Dictionary<string, Pixbuf> miniAvatars = new Dictionary<string, Pixbuf>();
		long      avatarSize = 0;
		string    avatarsPath;

		Pixbuf genericAvatar;
		Pixbuf smallGenericAvatar;
		Pixbuf miniGenericAvatar;
		
		public event EventHandler AvatarsChanged;

		public AvatarManager (Core core)
		{
		    this.core = core;

			genericAvatar = new Pixbuf(null, "Meshwork.Client.GtkClient.avatar-generic-large.png");
			smallGenericAvatar = new Pixbuf(null, "Meshwork.Client.GtkClient.avatar-generic-medium.png");
			miniGenericAvatar = new Pixbuf(null, "Meshwork.Client.GtkClient.avatar-generic-small.png");

			avatarsPath = Path.Combine (Settings.ConfigurationDirectory, "avatars");

			if (Directory.Exists (avatarsPath) == false) {
				Directory.CreateDirectory (avatarsPath);
			}

			foreach (Network network in core.Networks) {
				AddNetwork (network);
			}

			core.NetworkAdded += AddNetwork;

			UpdateMyAvatar ();
		}
		
		private void AddNetwork (Network network)
		{
			network.UpdateNodeInfo += (UpdateNodeInfoEventHandler)DispatchService.GuiDispatch(new UpdateNodeInfoEventHandler(network_UpdateNodeInfo));
			network.UserOffline    += (NodeOnlineOfflineEventHandler)DispatchService.GuiDispatch(new NodeOnlineOfflineEventHandler(network_UserOffline));
			network.ReceivedAvatar += (AvatarEventHandler)DispatchService.GuiDispatch(new AvatarEventHandler(network_ReceivedAvatar));
		
			network.LocalNode.AvatarSize = this.avatarSize;
		}

		public void UpdateMyAvatar ()
		{
			string myAvatarFile = Path.Combine(avatarsPath, string.Format("{0}.png", core.MyNodeID));

			if (File.Exists (myAvatarFile)) {

				var origPixbuf = new Pixbuf(myAvatarFile);
				avatars[core.MyNodeID] = origPixbuf;
				
				var pixbuf = origPixbuf.ScaleSimple(22,22, InterpType.Hyper);
				smallAvatars[core.MyNodeID] = pixbuf;
				
				pixbuf = origPixbuf.ScaleSimple(16, 16, InterpType.Hyper);
				miniAvatars[core.MyNodeID] = pixbuf;

				this.avatarSize = new FileInfo(myAvatarFile).Length;

				foreach (Network network in core.Networks) {
					network.LocalNode.AvatarSize = this.avatarSize;
				}
			} else {
				foreach (Network network in core.Networks) {
					network.LocalNode.AvatarSize = 0;
				}
			}
			
			if (AvatarsChanged != null) {
				AvatarsChanged(this, EventArgs.Empty);
			}
		}

		private void network_ReceivedAvatar (Network network, Node node, byte[] avatarData)
		{
			try {
				string dest = GetAvatarPath(node);

				using (FileStream stream = new FileStream(dest, FileMode.Create)) {
					stream.Write(avatarData, 0, avatarData.Length);
				}

				LoadAvatar(dest, node);
			} catch (Exception ex) {
				//TODO: What to do here?
				LoggingService.LogError(ex);
			}
		}

		private void network_UpdateNodeInfo (Network network, string oldnickname, Node node)
		{
			if (node == network.LocalNode) {
				return;
			}

			FileInfo existingFile = new FileInfo(GetAvatarPath(node));
			if (existingFile.Exists) {
				LoadAvatar(existingFile.FullName, node);
			}

			if (node.GetTrustedNode() != null && node.AvatarSize > 0 && (!existingFile.Exists || node.AvatarSize != existingFile.Length)) {
				network.RequestAvatar(node);
			}
		}

		private void LoadAvatar (string filePath, Node node)
		{
			// If we have never loaded this user's avatar this will do nothing
			RemoveAvatars(node.NodeID);

			if (File.Exists(filePath)) {
				var origAvatar = new Pixbuf(filePath);
				avatars.Add(node.NodeID, origAvatar);
				
				var pixbuf = origAvatar.ScaleSimple(22,22, InterpType.Hyper);
				smallAvatars.Add(node.NodeID, pixbuf);
				
				pixbuf = origAvatar.ScaleSimple(16, 16, InterpType.Hyper);
				miniAvatars.Add(node.NodeID, pixbuf);
			}
		}

		public string AvatarsPath {
			get {
				return avatarsPath;
			}
		}
		
		private void network_UserOffline (Network network, Node node)
		{
			if (node != network.LocalNode) {
				RemoveAvatars(node.NodeID);
			}
		}
		
		private void RemoveAvatars(string nodeid)
		{
			if (avatars.ContainsKey (nodeid) == true)
				avatars.Remove (nodeid);
			
			if (smallAvatars.ContainsKey (nodeid) == true)
				smallAvatars.Remove (nodeid);
			
			if (miniAvatars.ContainsKey(nodeid))
				miniAvatars.Remove(nodeid);

			if (AvatarsChanged != null)
				AvatarsChanged (this, EventArgs.Empty);
		}

		public Pixbuf GetAvatar (string nodeID)
		{
			if (avatars.ContainsKey(nodeID) == true)
				return avatars[nodeID];
			else
				return genericAvatar;
		}

		public Pixbuf GetAvatar (Node node)
		{
			return GetAvatar(node.NodeID);
		}

		public Pixbuf GetSmallAvatar (string nodeID)
		{
			if (smallAvatars.ContainsKey(nodeID) == true)
				return smallAvatars[nodeID];
			else
				return smallGenericAvatar;
		}

		public Pixbuf GetSmallAvatar (Node node)
		{
			return GetSmallAvatar(node.NodeID);
		}
		
		public Pixbuf GetMiniAvatar (string nodeID)
		{
			if (miniAvatars.ContainsKey(nodeID))
				return miniAvatars[nodeID];
			else
				return miniGenericAvatar;
		}
		
		public Pixbuf GetMiniAvatar (Node node)
		{
			return GetMiniAvatar(node.NodeID);
		}

		public byte[] GetAvatarBytes (string nodeId)
		{
			Pixbuf pixbuf = GetAvatar(nodeId);
			return pixbuf.SaveToBuffer("png");
		}

		public byte[] GetSmallAvatarBytes (string nodeId)
		{
			Pixbuf pixbuf = GetSmallAvatar(nodeId);
			return pixbuf.SaveToBuffer("png");
		}
		
		public byte[] GetAvatarBytes (Node node)
		{
			return GetAvatarBytes(node.NodeID);
		}

		public byte[] GetSmallAvatarBytes (Node node)
		{
			return GetSmallAvatarBytes(node.NodeID);
		}

		private string GetAvatarPath (Node node) 
		{
			return Path.Combine(avatarsPath, string.Format("{0}.png", node.NodeID));
		}
	}
}
