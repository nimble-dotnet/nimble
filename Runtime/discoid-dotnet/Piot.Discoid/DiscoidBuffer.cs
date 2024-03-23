/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/discoid-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Piot.Discoid
{
	/// <summary>
	/// Represents a specialized buffer that allows items to be set at any index without following a strict order.
	/// It supports dequeueing elements as long as there are no gaps in the sequence.
	/// </summary>
	/// <typeparam name="T">The type of elements stored in the buffer.</typeparam>

	public class DiscoidBuffer<T> : IEnumerable<T>
	{
		private T[] buffer;
		private bool[] isSet;
		private int front;
		private int capacity;

		/// <summary>
		/// Initializes a new instance of the DiscoidBuffer class with the specified capacity.
		/// </summary>
		/// <param name="capacity">The maximum number of elements the buffer can hold.</param>
		public DiscoidBuffer(int capacity)
		{
			buffer = new T[capacity];
			isSet = new bool[capacity];
			front = 0;
			this.capacity = capacity;
		}

		/// <summary>
		/// Sets the value at the specified index within the buffer. If the index exceeds the buffer capacity, an IndexOutOfRangeException is thrown.
		/// </summary>
		/// <param name="index">The zero-based index at which to set the value.</param>
		/// <param name="value">The value to set.</param>
		/// <exception cref="IndexOutOfRangeException">Thrown when the index is out of the bounds of the buffer capacity.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(int index, T value)
		{
			int bufferLen = capacity;
			if (index >= bufferLen)
			{
				throw new IndexOutOfRangeException($"discoid buffer: index {index} out of bounds");
			}

			int absoluteIndex = (front + index) % bufferLen;
			buffer[absoluteIndex] = value;
			isSet[absoluteIndex] = true;
		}

		/// <summary>
		/// Sets or gets the value at the specified index within the buffer.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <returns>The value at the specified index.</returns>
		public T this[int index]
		{
			set => Set(index, value);
		}

		/// <summary>
		/// Attempts to get the value at the specified index. Returns true if the item exists and is set; otherwise, false.
		/// </summary>
		/// <param name="index">The zero-based index of the item to retrieve.</param>
		/// <param name="value">When this method returns, contains the value of the item at the specified index,
		/// if the item exists and is set; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns>true if the item exists and is set; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGet(int index, out T value)
		{
			int bufferLen = capacity;
			if (index >= bufferLen)
			{
				value = default;
				return false;
			}

			int absoluteIndex = (front + index) % bufferLen;
			if (!isSet[absoluteIndex])
			{
				value = default;
				return false;
			}

			value = buffer[absoluteIndex];

			return true;
		}

		/// <summary>
		/// Discards a specified number of items from the front of the buffer.
		/// </summary>
		/// <param name="count">The number of items to discard.</param>
		/// <exception cref="InvalidOperationException">Thrown when attempting to discard more items than the buffer's capacity.</exception>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DiscardFront(int count)
		{
			if (count > capacity)
			{
				throw new InvalidOperationException("discoid buffer: discarding too much");
			}

			for (int i = 0; i < count; i++)
			{
				buffer[front] = default;
				isSet[front] = false;
				front = (front + 1) % capacity;
			}
		}

		/// <summary>
		/// Attempts to dequeue the item at the front of the buffer. Returns true if successful; otherwise, false.
		/// </summary>
		/// <param name="value">When this method returns, contains the dequeued item, if the method succeeded;
		/// otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
		/// <returns>true if the item was successfully dequeued; otherwise, false.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryDequeue(out T value)
		{
			var worked = TryGet(0, out value);
			if (!worked)
			{
				return false;
			}

			front = (front + 1) % capacity;
			return true;
		}


		/// <summary>
		/// Generates a bit mask representing the presence of elements in the buffer. Each bit corresponds to an element's set status.
		/// </summary>
		/// <returns>A <see cref="ulong"/> value where each bit represents the presence of an element at the corresponding index.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ulong Bits()
		{
			ulong bits = 0;
			int bufferLen = capacity;

			for (int i = 0; i < bufferLen; i++)
			{
				int index = (front + i) % bufferLen;
				if (!isSet[index])
				{
					continue;
				}

				bits |= 1UL << i;
			}

			return bits;
		}


		public IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}


		private class Enumerator : IEnumerator<T>
		{
			private readonly DiscoidBuffer<T> queue;
			private int currentIndex;
			private int lastIndex;

			public Enumerator(DiscoidBuffer<T> queue)
			{
				this.queue = queue;
				currentIndex = queue.front - 1;
				lastIndex = currentIndex % queue.capacity;
			}

			public T Current => queue.buffer[currentIndex];

			object IEnumerator.Current => Current;

			public void Dispose()
			{
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				var nextIndex = (currentIndex + 1) % queue.capacity;
				if (!queue.isSet[nextIndex])
				{
					return false;
				}

				if (nextIndex == lastIndex)
				{
					return false;
				}

				currentIndex = nextIndex;

				return true;
			}

			public void Reset()
			{
				currentIndex = queue.front - 1;
			}
		}
	}
}