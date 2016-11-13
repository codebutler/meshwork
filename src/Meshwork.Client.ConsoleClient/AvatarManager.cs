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
using Meshwork.Backend.Core;

namespace Meshwork.Client.Console
{
	public class AvatarManager : IAvatarManager
	{
		public event EventHandler AvatarsChanged;

	    private readonly Core core;

		Dictionary<string, byte[]> avatars = new Dictionary<string, byte[]>();
		int avatarSize = 0;

	    public AvatarManager (Core core)
		{
		    this.core = core;

			foreach (var network in core.Networks) {
				AddNetwork (network);
			}

			core.NetworkAdded += AddNetwork;

			UpdateMyAvatar ();
		}
		
		private void AddNetwork (Network network)
		{
			network.LocalNode.AvatarSize = this.avatarSize;
		}

		public void UpdateMyAvatar ()
		{
			var fileName = ((Settings)core.Settings).AvatarFile;
			if (fileName == null) {
				return;
			}

			var file = new FileInfo(fileName);

			if (file.Exists) {
				var buffer = new byte[file.Length];
				using (var stream = new FileStream(file.FullName, FileMode.Open)) {
					stream.Read(buffer, 0, (int)file.Length);
				}
				
				this.avatarSize = (int)file.Length;
				avatars[core.MyNodeID] = buffer;

				foreach (var network in core.Networks) {
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
