using System;
using System.Security.Cryptography;

namespace Meshwork.Backend.Core
{
    public class KeyManager
    {
        private readonly ISettings settings;
        private readonly bool _isKeyEncrypted;

        public KeyManager(ISettings settings)
        {
            this.settings = settings;
        }

        public RSAParameters EncryptionParameters
        {
            get {
                if (settings.PrivateKey == null) {
                    throw new InvalidOperationException();
                }
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(settings.PrivateKey);
                return rsa.ExportParameters(true);
            }
        }
    }
}