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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            head = 0;
            tail = 0;
            count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Peek()
        {
            if (count == 0)
            {
                throw new InvalidOperationException("Queue is empty");
            }

            return buffer[head];
        }

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

        public T Last => buffer[(tail - 1 + capacity) % capacity];

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

        public int Count => count;

        public int Capacity => capacity;

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