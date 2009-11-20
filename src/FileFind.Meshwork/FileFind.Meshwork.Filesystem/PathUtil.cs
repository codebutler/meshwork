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
		
		public static string GetBaseName (string path)
		{
			int i = path.LastIndexOf("/");
			return path.Substring(i + 1);
		}
		
		public static string GetParentPath (string path)
		{
			if (path.EndsWith("/")) path = path.Substring(0, path.Length - 1);
			int i = path.LastIndexOf("/");
			return path.Substring(0, i);
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
		
		public static Network GetNetwork (string path)
		{
			string[] parts = path.Split('/');
			Network network = Core.GetNetwork(parts[1]);
			if (network == null)
				throw new Exception("Network not found! " + path + " " + parts[1]);
			return network;
		}
		
		public static Node GetNode (string path)
		{
			string[] parts = path.Split('/');
			Network network = Core.GetNetwork(parts[1]);
			Node node = network.GetNode(parts[2]);
			if (node == null)
				throw new Exception("Not not found! " + path + " " + parts[2]);
			return node;
		}
	}
}
