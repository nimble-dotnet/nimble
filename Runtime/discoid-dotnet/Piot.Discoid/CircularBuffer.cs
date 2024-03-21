/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/discoid-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace Piot.Discoid
{
    /// <summary>
    /// Implements a generic circular buffer (ring buffer) with a fixed capacity.
    /// This collection allows for continuous insertion and removal of elements in a FIFO (First In First Out) manner,
    /// while maintaining constant time operations.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the buffer.</typeparam>
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private readonly T[] buffer;
        private readonly int capacity;
        private int head;
        private int tail;
        private int count;

        /// <summary>
        /// Initializes a new instance of the CircularBuffer class that is empty and has the specified capacity.
        /// </summary>
        /// <param name="capacity">The fixed size of the buffer.</param>
        /// <exception cref="ArgumentException">Thrown if the capacity is less than 1.</exception>
        public CircularBuffer(int capacity)
        {
            this.capacity = capacity;
            buffer = new T[capacity];
            head = 0;
            tail = 0;
            count = 0;
        }

        /// <summary>
        /// Adds an item to the end of the buffer. If the buffer is full, an InvalidOperationException is thrown.
        /// </summary>
        /// <param name="item">The item to add to the buffer.</param>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is full.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T item)
        {
            if (count == capacity)
            {
                throw new InvalidOperationException("Queue is full");
            }

            buffer[tail] = item;
            tail = (tail + 1) % capacity;
            count++;
        }

        /// <summary>
        /// Adds an item by reference to the end of the buffer. This method is intended for advanced scenarios where manipulating the item directly is needed.
        /// If the buffer is full, an InvalidOperationException is thrown.
        /// </summary>
        /// <returns>A reference to the added item in the buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is full.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T EnqueueRef()
        {
            if (count == capacity)
            {
                throw new InvalidOperationException("Queue is full");
            }
            var oldTail = tail;
            tail = (tail + 1) % capacity;
            count++;

            return ref buffer[oldTail];
        }

        /// <summary>
        /// Removes and returns the item at the beginning of the buffer. If the buffer is empty, an InvalidOperationException is thrown.
        /// </summary>
        /// <returns>The item that was removed from the buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            var dequeuedItem = buffer[head];
            head = (head + 1) % capacity;
            count--;
            return dequeuedItem;
        }


        /// <summary>
        /// Discards a specified number of items from the beginning of the buffer.
        /// </summary>
        /// <param name="discardCount">The number of items to discard.</param>
        /// <exception cref="InvalidOperationException">Thrown if there are not enough items to discard.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Discard(uint discardCount)
        {
            if (discardCount > count)
            {
                throw new InvalidOperationException("Not enough items to discard");
            }

            head = (head + (int)discardCount) % capacity;
            count -= (int)discardCount;
        }

        /// <summary>
        /// Clears all items from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            head = 0;
            tail = 0;
            count = 0;
        }

        /// <summary>
        /// Returns the item at the beginning of the buffer without removing it.
        /// </summary>
        /// <returns>The item at the beginning of the buffer.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the buffer is empty.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return buffer[head];
        }

        /// <summary>
        /// Returns an item at a specific index from the buffer without removing it.
        /// </summary>
        /// <param name="index">The zero-based index of the item to retrieve.</param>
        /// <returns>The item at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is out of range.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek(uint index)
        {
            if (index >= count)
            {
                throw new IndexOutOfRangeException("peek is out of range");
            }

            int bufferIndex = (head + (int)index) % capacity;
            return buffer[bufferIndex];
        }

        /// <summary>
        /// Gets the last item enqueued into the buffer.
        /// </summary>
        public T Last => buffer[(tail - 1 + capacity) % capacity];

        /// <summary>
        /// Gets or sets the item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to get or set.</param>
        /// <returns>The item at the specified index.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if the index is invalid.</exception>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                {
                    throw new IndexOutOfRangeException("Index is out of range");
                }

                int bufferIndex = (head + index) % capacity;
                return buffer[bufferIndex];
            }
        }

        /// <summary>
        /// Gets the number of items contained in the buffer.
        /// </summary>
        public int Count => count;

        /// <summary>
        /// Gets the capacity of the buffer.
        /// </summary>
        public int Capacity => capacity;

        /// <summary>
        /// Gets a value indicating whether the buffer is empty.
        /// </summary>
        public bool IsEmpty => count == 0;

        /// <summary>
        /// Gets a value indicating whether the buffer is full.
        /// </summary>
        public bool IsFull => count == capacity;

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a range of items in the buffer, starting at a specified offset.
        /// </summary>
        /// <param name="offset">The zero-based offset at which to start the enumeration.</param>
        /// <param name="itemCountToEnumerate">The number of items to enumerate.</param>
        /// <returns>An IEnumerator&lt;T&gt; for the specified range within the CircularBuffer.</returns>
        public IEnumerator<T> GetRangeEnumerator(uint offset, uint itemCountToEnumerate)
        {
            return new RangeEnumerator(this, (uint)((head + offset) % capacity), itemCountToEnumerate);
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (itemsEnumerated >= queue.count)
                {
                    return false;
                }

                currentIndex = (currentIndex + 1) % queue.capacity;
                itemsEnumerated++;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currentIndex = queue.head - 1;
                itemsEnumerated = 0;
            }
        }

        private class RangeEnumerator : IEnumerator<T>
        {
            private readonly CircularBuffer<T> queue;
            private int currentIndex;
            private uint itemsEnumerated;
            private readonly uint targetCount;
            private readonly int startIndex;

            public RangeEnumerator(CircularBuffer<T> queue, uint index, uint count)
            {
                this.queue = queue;
                startIndex = (int)index;
                currentIndex = (int)index - 1;
                itemsEnumerated = 0;
                targetCount = count;
                if (index >= queue.capacity)
                {
                    throw new ArgumentException("out of bounds", nameof(index));
                }

                if (count > queue.count)
                {
                    throw new ArgumentException("out of bounds", nameof(count));
                }
            }

            public T Current => queue.buffer[currentIndex];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                if (itemsEnumerated >= targetCount)
                {
                    return false;
                }

                currentIndex = (currentIndex + 1) % queue.capacity;
                itemsEnumerated++;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currentIndex = startIndex - 1;
                itemsEnumerated = 0;
            }
        }
    }
}