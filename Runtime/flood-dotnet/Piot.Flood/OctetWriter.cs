/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/flood-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Runtime.CompilerServices;

namespace Piot.Flood
{
    public sealed class OctetWriter : IOctetWriterWithResult, IOctetWriterOctetsLeft
    {
        readonly byte[] array;
        private int position;
        private int size;
        private int maxSize;

        public OctetWriter(uint size)
        {
            position = 0;
            array = new byte[size];
            this.size = (int)size;
        }

        public uint Position => (uint)position;
        public uint OctetsLeft => (uint)(size - position);

        public int Size => size;

        public int Tell
        {
            get
            {
                maxSize = position;
                return position;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int pos)
        {
            if (position > maxSize || position > size)
            {
                throw new Exception($"illegal seek {pos}");
            }

            position = pos;
        }

        public ReadOnlySpan<byte> Octets => array.AsSpan(0, position);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt8(byte value)
        {
            if (position + 1 > size)
            {
                throw new ArgumentOutOfRangeException($"writeUint8: wrote too much {position} out of {size}");
            }

            array[position++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt8(sbyte value)
        {
            if (position + 1 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteInt8: wrote too much {position} out of {size}");
            }

            array[position++] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt16(ushort value)
        {
            if (position + 2 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteUInt16: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 8) & 0xFF);
            array[position + 1] = (byte)(value & 0xFF);
            position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt16(short value)
        {
            if (position + 2 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteInt16: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 8) & 0xFF);
            array[position + 1] = (byte)(value & 0xFF);
            position += 2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt32(uint value)
        {
            if (position + 4 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteUInt32: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 24) & 0xFF);
            array[position + 1] = (byte)((value >> 16) & 0xFF);
            array[position + 2] = (byte)((value >> 8) & 0xFF);
            array[position + 3] = (byte)(value & 0xFF);
            position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt32(int value)
        {
            if (position + 4 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteInt32: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 24) & 0xFF);
            array[position + 1] = (byte)((value >> 16) & 0xFF);
            array[position + 2] = (byte)((value >> 8) & 0xFF);
            array[position + 3] = (byte)(value & 0xFF);
            position += 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUInt64(ulong value)
        {
            if (position + 8 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteUInt64: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 56) & 0xFF);
            array[position + 1] = (byte)((value >> 48) & 0xFF);
            array[position + 2] = (byte)((value >> 40) & 0xFF);
            array[position + 3] = (byte)((value >> 32) & 0xFF);
            array[position + 4] = (byte)((value >> 24) & 0xFF);
            array[position + 5] = (byte)((value >> 16) & 0xFF);
            array[position + 6] = (byte)((value >> 8) & 0xFF);
            array[position + 7] = (byte)(value & 0xFF);
            position += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt64(long value)
        {
            if (position + 8 > size)
            {
                throw new ArgumentOutOfRangeException($"WriteUInt64: wrote too much {position} out of {size}");
            }

            array[position] = (byte)((value >> 56) & 0xFF);
            array[position + 1] = (byte)((value >> 48) & 0xFF);
            array[position + 2] = (byte)((value >> 40) & 0xFF);
            array[position + 3] = (byte)((value >> 32) & 0xFF);
            array[position + 4] = (byte)((value >> 24) & 0xFF);
            array[position + 5] = (byte)((value >> 16) & 0xFF);
            array[position + 6] = (byte)((value >> 8) & 0xFF);
            array[position + 7] = (byte)(value & 0xFF);
            position += 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteOctets(ReadOnlySpan<byte> readOnlySpan)
        {
            if (position + readOnlySpan.Length > size)
            {
                throw new ArgumentOutOfRangeException(
                    $"WriteOctets: wrote too much {position}, octets:{readOnlySpan.Length} out of {size}");
            }

            readOnlySpan.CopyTo(array.AsSpan(position));
            position += readOnlySpan.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            position = 0;
        }
    }
}