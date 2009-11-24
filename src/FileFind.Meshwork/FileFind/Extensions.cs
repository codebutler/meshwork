using System;
using System.Linq;
using System.Collections.Generic;

namespace FileFind
{
	public static class Extensions
	{
		public static T[] Slice<T> (this T[] source, int start)
		{
			return Slice(source, start, -1);
		}

		public static T[] Slice<T> (this T[] source, int start, int count)
		{
			if (start < 0)
				start = source.Length + start;
			
			if (count > source.Length + start)
				count = source.Length - start;
			
			if (count < 0)
				count = source.Length - Math.Abs(count) - start + 1;
			
			T[] result = new T[count];
			for (int x = 0; x < count; x++)
				result[x] = source[start + x];
			return result;
		}

		// http://social.msdn.microsoft.com/Forums/en-US/csharpgeneral/thread/c4948bfb-bc07-4681-a977-ac84a0c34ede
		public static IEnumerable<string> CutIntoSetsOf (this string value, int interval)
		{
			int remainder = value.Length % interval;
			
			var result = new string[value.Length / interval + remainder / (1 > remainder ? 1 : remainder)];
			
			for (int i = 0; i < result.Length; i++) {
				yield return value.Substring(i * interval, interval < (value.Length - i * interval) ? interval : (value.Length - i * interval));
			}
		}

		public static string Join (this IEnumerable<string> source, string deliminer)
		{
			return String.Join(deliminer, source.ToArray());
		}

		// Below EnumSlice and SliceIterator functions from:
		// http://jacobcarpenter.wordpress.com/2007/11/16/ruby-inspired-extension-method/

		public static IEnumerable<T[]> EnumSlice<T> (this IEnumerable<T> sequence, int size)
		{
			// validate arguments
			if (sequence == null)
				throw new ArgumentNullException("sequence");
			if (size <= 0)
				throw new ArgumentOutOfRangeException("size");
			
			// return lazily evaluated iterator
			return SliceIterator(sequence, size);
		}

		private static IEnumerable<T[]> SliceIterator<T> (IEnumerable<T> sequence, int size)
		{
			// prepare the result array
			int position = 0;
			T[] resultArr = new T[size];
			
			foreach (T item in sequence) {
				// NOTE: performing the following test at the beginning of the loop ensures that we do not needlessly
				// create empty result arrays for sequences with even numbers of elements [(sequence.Count() % size) == 0]
				if (position == size) {
					// full result array; return to caller
					yield return resultArr;
					
					// create a new result array and reset position
					resultArr = new T[size];
					position = 0;
				}
				
				// store the current element in the result array
				resultArr[position++] = item;
			}
			
			// no elements in source sequence
			if (position == 0)
				yield break;
			
			// resize partial final slice
			if (position < size)
				Array.Resize(ref resultArr, position);
			
			// return final slice
			yield return resultArr;
		}
	}
}
