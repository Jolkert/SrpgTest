using System;
using System.Collections.Generic;

namespace Assets.Scripts
{
	public static class IEnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
		{
			foreach (T item in self)
				action(item);
		}
	}
}
