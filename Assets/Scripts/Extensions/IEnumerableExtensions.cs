using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
	public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
		{
			foreach (T item in self)
				action(item);
		}
		public static IEnumerable<T> Flatten<T>(this T[][] arr)
		{
			foreach (T[] subarr in arr)
				foreach (T item in subarr)
					yield return item;
		}
	}
}
