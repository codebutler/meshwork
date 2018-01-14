//
// IAvatarManager.cs:
//
// Authors:
//   Eric Butler <eric@codebutler.com>
//
// (C) 2006-2008 Meshwork Authors
//

using System;

namespace Meshwork.Backend.Core
{
	public interface IAvatarManager
	{
		//T GetAvatar (Node node);
		//T GetSmallAvatar (Node node);
		
		event EventHandler AvatarsChanged;

		void UpdateMyAvatar ();

		byte[] GetAvatarBytes (string nodeId);
		byte[] GetSmallAvatarBytes (string nodeId);
	}
}
