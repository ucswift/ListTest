using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ListTest
{
	/// <summary>
	/// Thread Safe Generic List implementation using locking
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class LockList<T> : IList<T>
	{
		// Underlying array holds the values
		private T[] _values;

		// Size of the stored items in the array (not the arrays size)
		private int _size;

		// SyncRoot for locking
		private Object _syncRoot;

		// For performence we will increase the size of the holding array by this number
		private const int Chunk = 8;

		#region Private Methods

		/// <summary>
		/// SyncRoot getter property, use for all locking operations
		/// </summary>
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
		/// The size of the underlying array
		/// </summary>
		public int Size
		{
			get
			{
				return _values.Length;
			}

			set
			{
				// Same size do nothing
				if (value == _values.Length)
					return;

				if (value > 0)
				{
					lock (SyncRoot)
					{
						T[] objs = new T[value];
						if (_size > 0)
							Array.Copy(_values, 0, objs, 0, _size);
						_values = objs;
					}
				}
			}
		}

		/// <summary>
		/// Size Array ensures the array has the capcity to hold items
		/// of the required size.
		/// </summary>
		/// <param name="requestedSize">The total size of the array</param>
		private void SizeArray(int requestedSize)
		{
			if (_values.Length >= requestedSize)
				return;

			lock (SyncRoot)
			{
				int newSize = _values.Length == 0 ? Chunk : _values.Length + Chunk;

				if (newSize < requestedSize)
					newSize = requestedSize;

				Size = newSize;
			}
		}
		#endregion Private Methods

		#region Constructor

		public LockList()
		{
			// Lock and initialize the array to 0
			lock (SyncRoot)
			{
				_values = new T[0];
			}
		}
		#endregion Constructor

		#region IList Implementation
		public IEnumerator<T> GetEnumerator()
		{
			// Not 100% sure this is correct, need to look at the documation more
			return (IEnumerator<T>)_values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			// ReSharper implementation not 100% sure it's correct
			return GetEnumerator();
		}

		public void Add(T item)
		{
			if (_size == _values.Length)
				SizeArray(_size + 1);

			_size++;
			_values[_size] = item;
		}

		public void Clear()
		{
			lock (SyncRoot)
			{
				_values = new T[0];
				_size = 0;
			}
		}

		public bool Contains(T item)
		{
			//http://stackoverflow.com/questions/13361005/c-sharp-generic-class-and-equalitycomparer
			var comparer = EqualityComparer<T>.Default;

			for (int i = 0; i < _size; i++)
			{
				if (comparer.Equals(_values[i], item))
					return true;
			}
			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Array.Copy(_values, 0, array, arrayIndex, _size);
		}

		public bool Remove(T item)
		{
			int index = IndexOf(item);

			if (index < 0)
				return false;

			RemoveAt(index);

			return true;
		}

		public int Count
		{
			get { return _size; }
		}

		public bool IsReadOnly
		{
			get { /* No clue here, returning false. */return false; }
		}

		public int IndexOf(T item)
		{
			return Array.IndexOf<T>(_values, item, 0, _size);
		}

		public void Insert(int index, T item)
		{
			SizeArray(_size + 1);

			lock (SyncRoot)
			{
				if (index < _size)
					Array.Copy(_values, index, _values, index + 1, _size - index);

				_values[index] = item;
				_size++;
			}
		}

		/// <summary>
		/// Remove an element at a specific index
		/// </summary>
		/// <param name="index">Index value to remove</param>
		public void RemoveAt(int index)
		{
			lock (SyncRoot)
			{
				_size--;

				if (index < _size)
					// Copies the array into the array starting at the index we want to remove
					Array.Copy(_values, index + 1, _values, index, _size - index);

				// Zero out the last value
				_values[_size] = default(T);
			}
		}

		/// <summary>
		/// Indexer access
		/// </summary>
		/// <param name="index">Base 0 array index position</param>
		/// <returns></returns>
		public T this[int index]
		{
			get
			{
				return _values[index];
			}
			set
			{
				_values[index] = value;
			}
		}
		#endregion IList Implementation
	}
}
