namespace Meshwork.Backend.Core.Protocol
{
    public struct SearchResultInfo
    {
        public int SearchId;
        public string[] Directories;
        public SharedFileListing[] Files;
    }
}