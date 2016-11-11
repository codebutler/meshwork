//
// ITransportEncryptor.cs:
//
// Authors:
//   Eric Butler <eric@extremeboredom.net>
//
// (C) 2008 FileFind.net (http://filefind.net)
//

namespace Meshwork.Backend.Core.Transport
{
	public interface ITransportEncryptor
	{
		void SetKey (byte[] keyBytes, byte[] ivBytes);
		
		byte[] Encrypt (byte[] buffer);
		byte[] Decrypt (byte[] buffer);
		
		int KeySize {
			get;
		}
		
		int IvSize {
			get;
		}
		
		int KeyExchangeLength {
			get;
		}

		bool Ready {
			get;
		}
	}
}
