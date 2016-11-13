using System;
using System.Collections.Generic;

namespace Meshwork.Backend.Core.Destination
{
    public static class DestinationTypeFriendlyNames
    {
        private static readonly Dictionary<string,string> FriendlyNames = new Dictionary<string,string>();

        public static void RegisterFriendlyName (Type type, string friendlyName)
        {
            FriendlyNames[type.ToString()] = friendlyName;
        }

        public static string GetFriendlyName (Type type)
        {
            return GetFriendlyName(type.ToString());
        }

        public static string GetFriendlyName (string typeName)
        {
            if (FriendlyNames.ContainsKey(typeName)) {
                return FriendlyNames[typeName];
            } else {
                return typeName;
            }
        }
    }
}