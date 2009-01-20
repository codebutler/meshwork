using System;

namespace FileFind.Meshwork.Filesystem
{
	public static class PathUtil
	{
		public static string GetDirectoryName (string path)
		{
			int last = path.LastIndexOf("/");

			if (last == 0) {
				last ++;
			}

			if (last > 0) {
				return path.Substring(0, last);
			} else {
				return path;
			}
		}

		public static string Join (string path1, string path2)
		{
			if (path1.EndsWith("/") ^ path2.StartsWith("/")) {
				return String.Format("{0}{1}", path1, path2);
			} else if (path1.EndsWith("/") && path2.StartsWith("/")) {
				return String.Format("{0}{1}", path1, path2.Substring(1));
			} else {
				return String.Format("{0}/{1}", path1, path2);
			}
		}

		public static bool AreEqual (string path1, string path2)
		{
			if (!path1.EndsWith("/")) {
				path1 += "/";
			}

			if (!path2.EndsWith("/")) {
				path2 += "/";
			}

			return (path1 == path2);
		}

		public static DirectoryType GetDirectoryType (string path)
		{
			if (path.StartsWith("/") == false) {
				throw new Exception("Invalid path: " + path);
			}

			if (path.EndsWith("/") == false) {
				path = path + "/";
			}
			
			string[] pathParts = path.Substring(0, path.Length - 1).Split('/');
			
			if (pathParts.Length == 1) {
				return DirectoryType.Root;
			} else if (pathParts.Length == 2) {
				if (pathParts[1] == Core.MyNodeID) {
					// This is me!
					return DirectoryType.Node;
				} else {
					return DirectoryType.Network;
				}
			} else if (pathParts.Length == 3) {
				if (pathParts[pathParts.Length - 2] == Core.MyNodeID) {
					// Directory in root of our share
					return DirectoryType.Normal;
				} else {
					return DirectoryType.Node;
				}
			} else {
				return DirectoryType.Normal;
			}
		}
	}

	public enum DirectoryType {
		Root,
		Node,
		Network,
		Normal
	}
}
