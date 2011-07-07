//
// AvatarManager.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

using System;
using System.Collections.Generic;
using System.IO;

namespace FileFind.Meshwork.DaemonClient
{
	public class AvatarManager : IAvatarManager
	{
		public event EventHandler AvatarsChanged;

		Dictionary<string, byte[]> avatars = new Dictionary<string, byte[]>();
		int avatarSize = 0;

		public AvatarManager ()
		{
			foreach (Network network in Core.Networks) {
				AddNetwork (network);
			}

			Core.NetworkAdded += AddNetwork;

			UpdateMyAvatar ();
		}
		
		private void AddNetwork (Network network)
		{
			network.LocalNode.AvatarSize = this.avatarSize;
		}

		public void UpdateMyAvatar ()
		{
			string fileName = ((Settings)Core.Settings).AvatarFile;
			if (fileName == null) {
				return;
			}

			FileInfo file = new FileInfo(fileName);

			if (file.Exists) {
				byte[] buffer = new byte[file.Length];
				using (FileStream stream = new FileStream(file.FullName, FileMode.Open)) {
					stream.Read(buffer, 0, (int)file.Length);
				}
				
				this.avatarSize = (int)file.Length;
				avatars[Core.MyNodeID] = buffer;

				foreach (Network network in Core.Networks) {
					network.LocalNode.AvatarSize = this.avatarSize;
				}

				if (AvatarsChanged != null) {
					AvatarsChanged(this, EventArgs.Empty);
				}
			}
		}

		public byte[] GetAvatarBytes (string nodeId)
		{
			return avatars[nodeId];
		}
		
		public byte[] GetSmallAvatarBytes (string nodeId)
		{
			throw new NotImplementedException();
		}
	}
}
