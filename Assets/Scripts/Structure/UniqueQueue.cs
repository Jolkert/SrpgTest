using System;
using System.Collections;
using System.Collections.Generic;

namespace Assets.Scripts.Structure
{
	public class UniqueQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
	{
		private HashSet<T> _hashSet;
		private Queue<T> _queue;

		public int Count => _hashSet.Count;

		public UniqueQueue()
		{
			_hashSet = new();
			_queue = new();
		}

		public void Enqueue(T item)
		{
			if (_hashSet.Add(item))
				_queue.Enqueue(item);
		}
		public T Dequeue()
		{
			T item = _queue.Dequeue();
			_hashSet.Remove(item);

			return item;
		}
		public T Peek() => _queue.Peek();

		public void Clear()
		{
			_queue.Clear();
			_hashSet.Clear();
		}

		public IEnumerator<T> GetEnumerator() => _queue.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}
	}
}
