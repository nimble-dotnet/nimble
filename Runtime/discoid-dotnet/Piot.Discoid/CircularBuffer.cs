/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/discoid-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Piot.Discoid
{
	public class CircularBuffer<T> : IEnumerable<T>
	{
		private readonly T[] buffer;
		private readonly int capacity;
		private int head;
		private int tail;
		private int count;

		public CircularBuffer(int capacity)
		{
			this.capacity = capacity;
			buffer = new T[capacity];
			head = 0;
			tail = 0;
			count = 0;
		}

		public void Enqueue(T item)
		{
			if(count == capacity)
			{
				throw new InvalidOperationException("Queue is full");
			}

			buffer[tail] = item;
			tail = (tail + 1) % capacity;
			count++;
		}

		public T Dequeue()
		{
			if(count == 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			var dequeuedItem = buffer[head];
			head = (head + 1) % capacity;
			count--;
			return dequeuedItem;
		}

		public void Discard(uint discardCount)
		{
			if(discardCount > count)
			{
				throw new InvalidOperationException("Not enough items to discard");
			}

			head = (head + (int)discardCount) % capacity;
			count -= (int)discardCount;
		}

		public void Clear()
		{
			head = 0;
			tail = 0;
			count = 0;
		}

		public T Peek()
		{
			if(count == 0)
			{
				throw new InvalidOperationException("Queue is empty");
			}

			return buffer[head];
		}

		public T Peek(uint index)
		{
			if(index >= count)
			{
				throw new IndexOutOfRangeException("peek is out of range");
			}

			int bufferIndex = (head + (int)index) % capacity;
			return buffer[bufferIndex];
		}

		public T this[int index]
		{
			get
			{
				if(index < 0 || index >= count)
				{
					throw new IndexOutOfRangeException("Index is out of range");
				}

				int bufferIndex = (head + index) % capacity;
				return buffer[bufferIndex];
			}
		}

		public int Count => count;

		public bool IsEmpty => count == 0;

		public bool IsFull => count == capacity;

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
			private readonly CircularBuffer<T> queue;
			private int currentIndex;
			private int itemsEnumerated;

			public Enumerator(CircularBuffer<T> queue)
			{
				this.queue = queue;
				currentIndex = queue.head - 1;
				itemsEnumerated = 0;
			}

			public T Current => queue.buffer[currentIndex];

			object IEnumerator.Current => Current;

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if(itemsEnumerated >= queue.count)
				{
					return false;
				}

				currentIndex = (currentIndex + 1) % queue.capacity;
				itemsEnumerated++;
				return true;
			}

			public void Reset()
			{
				currentIndex = queue.head;
				itemsEnumerated = 0;
			}
		}
	}
}