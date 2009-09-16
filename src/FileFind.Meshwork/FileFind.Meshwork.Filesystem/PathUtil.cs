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
	}
}
