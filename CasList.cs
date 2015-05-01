using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	/// In this implenetation I'm trying limit larger locks and instead lock for smaller atomic
	/// operations when I can. Not all operations I could find an amotic (Interlocked) option
	/// for so I used a lower level version of lock (Monitor.Enter/Leave) to use there. 
	/// 
	/// I researched this implementation by looking at ConcurrentBag and ConcurrentStack 
	/// implementations and adapted LockList. Trying to find another solution then standard
	/// locking implementation.
	/// </remarks>
	/// <typeparam name="T"></typeparam>
	public class CasList<T> : IList<T>
	{
		#region Private Vairables
		// Underlying array holds the values
		private volatile Node[] _values;

		// Size of the stored items in the array (not the arrays size)
		private int _size;

		// SyncRoot for locking
		private Object _syncRoot;

		// For performence we will increase the size of the holding array by this number
		private const int Chunk = 8;
		#endregion Private Vairables

		public CasList()
		{
			_values = new Node[0];
			_size = 0;
		}

		#region Private Methods
		private Object SyncRoot
		{
			get
			{
				// CAS way of creating the sync root
				if (_syncRoot == null)
					Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);

				return _syncRoot;
			}
		}

		/// <summary>
		/// Verifies the array is the needed size, if it's too small then increases
		/// the size of value array
		/// </summary>
		/// <param name="requestedSize"></param>
		private void SizeArray(int requestedSize)
		{
			if (_values.Length >= requestedSize)
				return;

			// Increase the array size by the Chunk size
			int newSize = _values.Length == 0 ? Chunk : _values.Length + Chunk;

			// If our new size is still smaller then  our requested size just size it to that
			if (newSize < requestedSize)
				newSize = requestedSize;

			// If were making a size change
			if (newSize > 0)
			{
				Node[] objs = new Node[newSize];

				// Do we have elements to copy?
				if (_size > 0)
				{
					bool wasLocked = false;
					try
					{
						Monitor.Enter(SyncRoot, ref wasLocked);

						// Copy our existing array objects into the new array
						Array.Copy(_values, 0, objs, 0, _size);
						_values = objs;
					}
					finally
					{
						if (wasLocked)
							Monitor.Exit(SyncRoot);
					}
				}
				else
				{
					bool wasLocked = false;
					try
					{
						Monitor.Enter(SyncRoot, ref wasLocked);

						// Just set to the empty array
						_values = objs;
					}
					finally
					{
						if (wasLocked)
							Monitor.Exit(SyncRoot);
					}
				}
			}
		}
		#endregion Private Methods

		#region IList Implementation
		public IEnumerator<T> GetEnumerator()
		{
			bool wasLocked = false;
			var newList = new List<T>();

			try
			{
				Monitor.Enter(SyncRoot, ref wasLocked);

				// Pull out our values into a new list
				newList = new List<T>(_values.Select(x => x.Value));
			}
			finally
			{
				if (wasLocked)
					Monitor.Exit(SyncRoot);
			}

			// Get the enumerator 
			return newList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			// Use a spin wait, as it's much more performant for very quick wait's without
			// turning over execution
			SpinWait spinWait = new SpinWait();
			var newValue = new Node(item);

			// Somewhat dervied from a framework concurrent queue/bag implementation
			do
			{
				spinWait.SpinOnce();

				var size = Thread.VolatileRead(ref _size);
				if (size == _values.Length)
					SizeArray(size + 1);
			} // CAS addition here to make adding items more performant then a lock
			while (Interlocked.CompareExchange<Node>(ref _values[Thread.VolatileRead(ref _size)], newValue, null) != null);

			Interlocked.Increment(ref _size);
		}

		public void Clear()
		{
			bool wasLocked = false;

			try
			{
				Monitor.Enter(SyncRoot, ref wasLocked);

				// Reset the array and the size
				_values = new Node[0];
				Interlocked.Exchange(ref _size, 0);
			}
			finally
			{
				if (wasLocked)
					Monitor.Exit(SyncRoot);
			}
		}

		public bool Contains(T item)
		{
			//http://stackoverflow.com/questions/13361005/c-sharp-generic-class-and-equalitycomparer
			var comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < Volatile.Read(ref _size); i++)
			{
				var v = Volatile.Read(ref _values[i]);
				if (comparer.Equals(v.Value, item))
					return true;
			}

			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			bool wasLocked = false;

			try
			{
				Monitor.Enter(SyncRoot, ref wasLocked);

				for (int i = 0; i < Volatile.Read(ref _size); i++)
				{
					// May need to implement a deep copy here
					array[i] = _values[i].Value;
				}
			}
			finally
			{
				if (wasLocked)
					Monitor.Exit(SyncRoot);
			}
		}

		public bool Remove(T item)
		{
			// Find the index of the item
			int index = IndexOf(item);

			// If didn't find anything return
			if (index < 0)
				return false;

			// Else remove the item at the index
			RemoveAt(index);

			return true;
		}

		public int Count
		{
			// Use Volatile.Read as it tries to 'guarentee' fressness by using a memory barrier
			get { return Volatile.Read(ref _size); }
		}

		public bool IsReadOnly
		{
			// Do we currently have a lock, if so were ReadOnly
			get { return Monitor.TryEnter(SyncRoot, 0); }
		}

		public int IndexOf(T item)
		{
			//http://stackoverflow.com/questions/13361005/c-sharp-generic-class-and-equalitycomparer
			var comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < Volatile.Read(ref _size); i++)
			{
				var v = Volatile.Read(ref _values[i]);
				if (comparer.Equals(v.Value, item))
					return i;
			}

			return -1;
		}

		public void Insert(int index, T item)
		{
			SizeArray(_size + 1);

			if (index < _size)
			{
				bool wasLocked = false;

				try
				{
					Monitor.Enter(SyncRoot, ref wasLocked);

					Array.Copy(_values, index, _values, index + 1, _size - index);
					_values[index] = new Node(item);
				}
				finally
				{
					if (wasLocked)
						Monitor.Exit(SyncRoot);
				}
			}

			Interlocked.Increment(ref _size);
		}

		public void RemoveAt(int index)
		{
			// Decrease our array size
			Interlocked.Decrement(ref _size);

			if (index < Volatile.Read(ref _size))
			{
				bool wasLocked = false;

				try
				{
					Monitor.Enter(SyncRoot, ref wasLocked);

					// Copies the array into the array starting at the index we want to remove
					Array.Copy(_values, index + 1, _values, index, Volatile.Read(ref _size) - index);

					// Zero out the last value
					_values[_size] = null;
				}
				finally
				{
					if (wasLocked)
						Monitor.Exit(SyncRoot);
				}
			}
		}

		public T this[int index]
		{
			get
			{
				if (index > Volatile.Read(ref _size) || index < 0)
					throw new IndexOutOfRangeException();

				return _values[index].Value;
			}
			set
			{
				if (index > Volatile.Read(ref _size) || index < 0)
					throw new IndexOutOfRangeException();

				_values[index] = new Node(value);

				Monitor.PulseAll(_values);
			}
		}
		#endregion IList Implementation

		#region Internal Models
		/// <summary>
		/// Node to allow us to use CAS on array elements
		/// </summary>
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
