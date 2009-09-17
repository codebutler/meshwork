using System;

namespace FileFind
{
	public static class Extensions
	{
		public static T[] Slice<T> (this T[] source, int start)
		{
			return Slice (source, start, -1);
		}

		public static T[] Slice<T> (this T[] source, int start, int count)
		{
			if (start < 0)
				start = source.Length + start;
			
			if (count > source.Length + start)
				count = source.Length - start;
			
			if (count < 0)
				count = source.Length - Math.Abs (count) - start + 1;
			
			T[] result = new T[count];
			for (int x = 0; x < count; x++)
				result[x] = source[start + x];
			return result;
		}
	}
}