// $Id: CRCStandard.cs 14 2006-02-18 01:16:35Z jay $

#region License
/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is Classless.Hasher - C#/.NET Hash and Checksum Algorithm Library.
 *
 * The Initial Developer of the Original Code is Classless.net.
 * Portions created by the Initial Developer are Copyright (C) 2004 the Initial
 * Developer. All Rights Reserved.
 *
 * Contributor(s):
 *		Jason Simeone (jay@classless.net)
 * 
 * ***** END LICENSE BLOCK ***** */
#endregion

namespace Meshwork.Library.CRC {
	/// <summary>Predefined standards for CRC algorithms.</summary>
	public enum CRCStandard {
		/// <summary>The standard CRC8 algorithm.</summary>
		CRC8,

		/// <summary>The IBM standard CRC16 algorithm.</summary>
		CRC16_IBM,

		/// <summary>The CCITT standard CRC16 algorithm. Used in things such as X.25, SDLC, and HDLC.</summary>
		CRC16_CCITT,

		/// <summary>A variation on the CRC16 algorithm. Used in ARC.</summary>
		CRC16_ARC,

		/// <summary>A variation on the CRC16 algorithm. Used in XMODEM.</summary>
		CRC16_XMODEM,

		/// <summary>A variation on the CRC16 algorithm. Used in ZMODEM.</summary>
		CRC16_ZMODEM,

		/// <summary>The standard CRC24 algorithm. Used in things such as PGP.</summary>
		CRC24,

		/// <summary>The standard CRC32 algorithm. Used in things such as PKZip, SFV, AUTODIN II, Ethernet, and FDDI.</summary>
		CRC32,

		/// <summary>A variation on the CRC32 algorithm. Used in JAMCRC.</summary>
		CRC32_JAMCRC,

		/// <summary>A variation on the CRC32 algorithm. Used in BZip2.</summary>
		CRC32_BZIP2,

		/// <summary>The ISO standard CRC64 algorithm.</summary>
		CRC64_ISO,

		/// <summary>The ECMA standard CRC64 algorithm.</summary>
		CRC64_ECMA,
	}
}
