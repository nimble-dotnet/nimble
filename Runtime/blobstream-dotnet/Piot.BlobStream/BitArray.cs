/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;

namespace Piot.BlobStream
{
    /// <summary>
    /// Represents a fixed-size sequence of bits.
    /// </summary>
    public class BitArray
    {
        private readonly ulong[] array;
        private readonly int atomCount;
        private int bitCount;
        private int numberOfBitsSet;

        private const int BIT_ARRAY_BITS_IN_ATOM = 64;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class.
        /// </summary>
        /// <param name="bitCount">The total number of bits in the array.</param>
        public BitArray(int bitCount)
        {
            var calculatedAtomCount = (bitCount + (BIT_ARRAY_BITS_IN_ATOM - 1)) / BIT_ARRAY_BITS_IN_ATOM;
            array = new ulong[atomCount];
            atomCount = calculatedAtomCount;
            this.bitCount = bitCount;
            numberOfBitsSet = 0;
        }

        /// <summary>
        /// Gets the number of bits in the bit array.
        /// </summary>
        public int Length => bitCount;

        /// <summary>
        /// Resets all bits in the array to false.
        /// </summary>
        public void Reset()
        {
            Array.Clear(array, 0, array.Length);
            bitCount = 0;
            numberOfBitsSet = 0;
        }

        /// <summary>
        /// Checks whether all bits in the array are set to true.
        /// </summary>
        /// <returns><c>true</c> if all bits are set; otherwise, <c>false</c>.</returns>
        public bool AreAllSet()
        {
            return bitCount == numberOfBitsSet;
        }

        /// <summary>
        /// Finds the index of the first bit that is not set.
        /// </summary>
        /// <returns>The index of the first bit that is unset, or the bit count if all bits are set.</returns>
        public int FirstUnset()
        {
            for (var i = 0; i < atomCount; ++i)
            {
                if (array[i] == ulong.MaxValue)
                {
                    continue;
                }

                var accumulator = array[i];
                for (var bit = 0; bit < BIT_ARRAY_BITS_IN_ATOM; ++bit)
                {
                    ulong mask = 1U << bit;
                    if ((accumulator & mask) == 0)
                    {
                        return i * BIT_ARRAY_BITS_IN_ATOM + bit;
                    }
                }
            }

            return bitCount;
        }

        /// <summary>
        /// Sets the bit at the specified index to true.
        /// </summary>
        /// <param name="index">The zero-based index of the bit to set.</param>
        /// <exception cref="ArgumentException">Thrown when the index is out of range.</exception>
        public void Set(int index)
        {
            if (index >= bitCount)
            {
                throw new ArgumentException("Index out of range", nameof(index));
            }

            var arrayIndex = index / BIT_ARRAY_BITS_IN_ATOM;
            var bitIndex = index % BIT_ARRAY_BITS_IN_ATOM;
            ulong mask = 1U << bitIndex;

            if ((array[arrayIndex] & mask) == 0)
            {
                numberOfBitsSet++;
            }

            array[arrayIndex] |= mask;
        }
        
        /// <summary>
        /// Indexer to get the value of the bit at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the bit.</param>
        /// <returns><c>true</c> if the bit at the specified index is set; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Thrown when the index is out of range.</exception>
        public bool this[int index]
        {
            get
            {
                if (index >= bitCount)
                {
                    throw new ArgumentException("Index out of range", nameof(index));
                }

                var arrayIndex = index / BIT_ARRAY_BITS_IN_ATOM;
                var bitIndex = index % BIT_ARRAY_BITS_IN_ATOM;

                return (array[arrayIndex] & (1U << bitIndex)) != 0; 
            }
        }

        public override string ToString()
        {
            return $"[bitarray {numberOfBitsSet}/{Length}]";
        }
    }
}
