using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Scripts.Structure
{
	public class PeekableRandom
	{
		private Random _random;
		private Queue<int> _queue = new Queue<int>();

		public PeekableRandom()
		{
			_random = new Random();
		}
		public PeekableRandom(int seed)
		{
			_random = new Random(seed);
		}

		public int Generate()
		{
			int rn = _random.Next(0, 99);
			_queue.Enqueue(rn);
			return rn;
		}
		public int[] Generate(int count)
		{
			int[] generated = new int[count];
			for (int i = 0; i < count; i++)
				generated[i] = Generate();

			return generated;
		}


		public int Next()
		{
			if (_queue.Count > 0)
				return _queue.Dequeue();
			else
				return _random.Next(0, 99);
		}

		public int Peek()
		{
			if (_queue.Count == 0)
				return Generate();
			else
				return _queue.Peek();
		}
		public IEnumerable<int> Peek(int count)
		{
			if (_queue.Count < count)
			{
				int missingCount = count - _queue.Count;
				for (int i = 0; i < missingCount; i++)
					Generate();
			}

			return _queue.Take(count);
		}

	}
}
