using System;

namespace FileFind
{
	public static class Extensions
	{
		public static T[] Slice<T>(this T[] source, int start)
		{
			return Slice(source, start, source.Length - start);
		}

		public static T[] Slice<T>(this T[] source, int start, int count)
		{
			if (count > (source.Length - start))
				throw new ArgumentException("count is too big");
			T[] result = new T[count];
			for (int x = 0; x < count; x++)
				result[x] = source[x + start];
			return result;
		}
	}
}
