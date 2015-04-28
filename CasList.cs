using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Threading;

namespace ListTest
{
	/// <summary>
	/// Thread safe list implemtnation using CAS http://en.wikipedia.org/wiki/Compare-and-swap
	/// </summary>
	/// <remarks>
	/// This list implementation is Thead safe due to the fact that we CAS (Interlocked) to
	/// perform the atomic operations instead of locking. CAS operations are far more performant
	/// then compared to standard locking but are not 100% fool proof. 
	/// 
	/// In this implementaiton I marked the underlying array as volitile which is a keyword to
	/// prevent compiler optimizations that favor single threaded access.
	/// 
	/// In this implenetation I'm trying 
	/// 
	/// I researched this implementation by looking at ConcurrentBag and ConcurrentStack 
	/// implementations and adapted LockList.
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public class CasList<T> : IList<T>
	{
		#region Private Vairables
		// Underlying array holds the values
		private volatile Node[] _values;

		// Size of the stored items in the array (not the arrays size)
		private int _size;

		// For performence we will increase the size of the holding array by this number
		private const int Chunk = 8;
		#endregion Private Vairables

		#region Private Methods
		private void SizeArray(int requestedSize)
		{
			if (_values.Length >= requestedSize)
				return;

			int newSize = _values.Length == 0 ? Chunk : _values.Length + Chunk;

			if (newSize < requestedSize)
				newSize = requestedSize;

			if (newSize > 0)
			{
				Node[] objs = new Node[newSize];

				if (_size > 0)
				{
					bool wasLocked = false;

					try
					{
						Monitor.Enter(_values, ref wasLocked);
						Array.Copy(_values, 0, objs, 0, _size);
						_values = objs;
					}
					finally
					{
						if (wasLocked)
							Monitor.Exit(_values);
					}
				}
			}
		}
		#endregion Private Methods

		#region IList Implementation
		public IEnumerator<T> GetEnumerator()
		{
			bool wasLocked = false;
			List<T> newList = null;

			try
			{
				Monitor.Enter(_values, ref wasLocked);
				newList = new List<T>(_values.Select(x => x.Value));
			}
			finally
			{
				if (wasLocked)
					Monitor.Exit(_values);
			}

			return newList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			SpinWait spinWait = new SpinWait();
			var newValue = new Node(item);

			do
			{
				spinWait.SpinOnce();

				var size = Thread.VolatileRead(ref _size);
				if (size == _values.Length)
					SizeArray(size + 1);
			}
			while (Interlocked.CompareExchange<Node>(ref _values[Thread.VolatileRead(ref _size)], newValue, null) != null);

			Interlocked.Increment(ref _size);
		}

		public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(T item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove(T item)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsReadOnly
		{
			get { throw new NotImplementedException(); }
		}

		public int IndexOf(T item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, T item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public T this[int index]
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
		#endregion IList Implementation

		#region Internal Models
		internal class Node
		{
			public T Value { private set; get; }

			public Node(T value)
			{
				Value = value;
			}

		}
		#endregion Internal Models
	}
}
