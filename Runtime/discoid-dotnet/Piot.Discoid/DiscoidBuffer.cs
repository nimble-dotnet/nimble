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
	/// A buffer where it is possible to set items to index in any order
	/// Buffer can be dequeued as long as there is no gap to the next item
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DiscoidBuffer<T> : IEnumerable<T>
	{
		private T[] buffer;
		private bool[] isSet;
		private int front;
		private int capacity;

		public DiscoidBuffer(int capacity)
		{
			buffer = new T[capacity];
			isSet = new bool[capacity];
			front = 0;
			this.capacity = capacity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Set(int index, T value)
		{
			int bufferLen = capacity;
			if(index >= bufferLen)
			{
				throw new IndexOutOfRangeException($"discoid buffer: index {index} out of bounds");
			}

			int absoluteIndex = (front + index) % bufferLen;
			buffer[absoluteIndex] = value;
			isSet[absoluteIndex] = true;
		}

		public T this[int index]
		{
			set => Set(index, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGet(int index, out T value)
		{
			int bufferLen = capacity;
			if(index >= bufferLen)
			{
				value = default;
				return false;
			}

			int absoluteIndex = (front + index) % bufferLen;
			if(!isSet[absoluteIndex])
			{
				value = default;
				return false;
			}

			value = buffer[absoluteIndex];

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void DiscardFront(int count)
		{
			if(count > capacity)
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryDequeue(out T value)
		{
			var worked = TryGet(0, out value);
			if(!worked)
			{
				return false;
			}

			front = (front + 1) % capacity;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ulong Bits()
		{
			ulong bits = 0;
			int bufferLen = capacity;

			for (int i = 0; i < bufferLen; i++)
			{
				int index = (front + i) % bufferLen;
				if(!isSet[index])
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
				if(!queue.isSet[nextIndex])
				{
					return false;
				}

				if(nextIndex == lastIndex)
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