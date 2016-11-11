using System;

namespace Meshwork.Backend.Core.Protocol
{
    [Serializable]
    public struct SearchResultInfo
    {
        public int SearchId;
        public string[] Directories;
        public SharedFileListing[] Files;
    }
}