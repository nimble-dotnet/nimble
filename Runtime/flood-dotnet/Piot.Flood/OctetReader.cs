/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/flood-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Buffers.Binary;

namespace Piot.Flood
{
    public sealed class OctetReader : IOctetReaderWithSeekAndSkip
    {
        readonly byte[] array;
        private int pos;
        private int size;

        public OctetReader(ReadOnlySpan<byte> span)
        {
            array = span.ToArray();
            size = array.Length;
        }

        public byte ReadUInt8()
        {
            if (pos + 1 > size)
            {
                throw new Exception($"ReadUInt8: read too far {pos} out of {size}");
            }

            return array[pos++];
        }

        public sbyte ReadInt8()
        {
            if (pos + 1 > size)
            {
                throw new Exception($"ReadUInt8: read too far {pos} out of {size}");
            }

            return (sbyte)array[pos++];
        }

        public ushort ReadUInt16()
        {
            pos += 2;
            if (pos > size)
            {
                throw new Exception($"ReadUInt16: read too far {pos} out of {size}");
            }

            return (ushort)((array[pos - 2] << 8) | array[pos - 1]);
        }

        public short ReadInt16()
        {
            pos += 2;
            if (pos > size)
            {
                throw new Exception($"ReadInt16: read too far {pos} out of {size}");
            }

            return (short)((array[pos - 2] << 8) | array[pos - 1]);
        }

        public uint ReadUInt32()
        {
            pos += 4;
            if (pos > size)
            {
                throw new Exception($"ReadUInt32: read too far {pos} out of {size}");
            }

            return ((uint)array[pos - 4] << 24) | ((uint)array[pos - 3] << 16) | ((uint)array[pos - 2] << 8) |
                   array[pos - 1];
        }

        public int ReadInt32()
        {
            pos += 4;
            if (pos > size)
            {
                throw new Exception($"ReadInt32: read too far {pos} out of {size}");
            }

            return (array[pos - 4] << 24) | (array[pos - 3] << 16) | (array[pos - 2] << 8) |
                   array[pos - 1];
        }


        public ulong ReadUInt64()
        {
            pos += 8;

            if (pos > size)
            {
                throw new Exception($"ReadUInt64: read too far {pos} out of {size}");
            }

            return ((ulong)array[pos - 8] << 56) | ((ulong)array[pos - 7] << 48) | ((ulong)array[pos - 6] << 40) |
                   ((ulong)array[pos - 5] << 32) |
                   ((ulong)array[pos - 4] << 24) | ((ulong)array[pos - 3] << 16) | ((ulong)array[pos - 2] << 8) |
                   array[pos - 1];
        }

        public long ReadInt64()
        {
            pos += 8;

            if (pos > size)
            {
                throw new Exception($"ReadUInt64: read too far {pos} out of {size}");
            }

            return ((long)array[pos - 8] << 56) | ((long)array[pos - 7] << 48) | ((long)array[pos - 6] << 40) |
                   ((long)array[pos - 5] << 32) |
                   ((long)array[pos - 4] << 24) | ((long)array[pos - 3] << 16) | ((long)array[pos - 2] << 8) |
                   array[pos - 1];
        }


        public ReadOnlySpan<byte> ReadOctets(int octetCount)
        {
            pos += octetCount;

            if (pos > size)
            {
                throw new Exception($"ReadOctets: read too far {pos} {octetCount} out of {size}");
            }

            return array.AsSpan(pos - octetCount, octetCount);
        }

        public void Skip(int octetCount)
        {
            pos += octetCount;
            if (pos > size)
            {
                throw new($"skipped too far {octetCount} {pos} {size}");
            }
        }

        public ulong Position => (ulong)pos;

        public void Seek(ulong position)
        {
            pos = (int)position;
            if (pos >= array.Length)
            {
                throw new($"seek too far {pos} vs {array.Length}");
            }
        }

        public void Dispose()
        {
            // Intentionally blank
        }
    }
}