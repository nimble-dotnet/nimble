/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Flood;

namespace Piot.Raff
{
    public static class DeSerialize
    {
        /// <summary>
        ///     Must be the first things read from the stream or file
        /// </summary>
        /// <param name="reader"></param>
        /// <exception cref="Exception"></exception>
        public static void ReadHeader(IOctetReader reader)
        {
            var readHeader = reader.ReadOctets(Constants.fileHeader.Length);
            if (!readHeader.SequenceEqual(Constants.fileHeader))
            {
                throw new("illegal header");
            }
        }

        static FourCC ReadFourCC(IOctetReader reader)
        {
            var octets = reader.ReadOctets(4);
            return new(octets.ToArray());
        }

        public static uint ReadChunkHeader(IOctetReader reader, out FourCC icon, out FourCC name)
        {
            icon = ReadFourCC(reader);
            name = ReadFourCC(reader);
            return reader.ReadUInt32();
        }

        public static ChunkHeader ReadChunkHeader(IOctetReader reader)
        {
            var octetLength = ReadChunkHeader(reader, out var icon, out var name);

            return new(icon, name, octetLength);
        }

        public static uint ReadExpectedChunkHeader(IOctetReader reader, FourCC expectedIcon, FourCC expectedName)
        {
            var octetCount = ReadChunkHeader(reader, out var readIcon, out var readName);
            if (!readIcon.Value.Equals(expectedIcon.Value))
            {
                throw new($"not equal raff chunk icon. expected {expectedIcon}, but encountered {readIcon}");
            }

            if (!readName.Value.Equals(expectedName.Value))
            {
                throw new($"not equal raff chunk name. expected {expectedName}, but encountered {readName}");
            }

            return octetCount;
        }

        public static ReadOnlySpan<byte> ReadChunk(IOctetReader reader, out FourCC icon, out FourCC name)
        {
            var octetLength = ReadChunkHeader(reader, out icon, out name);
            return reader.ReadOctets((int)octetLength);
        }

        public static ReadOnlySpan<byte> ReadExpectedChunk(IOctetReader reader, FourCC expectedIcon,
            FourCC expectedName)
        {
            var octets = ReadChunk(reader, out var icon, out var name);
            if (!icon.Value.Equals(expectedIcon.Value))
            {
                throw new("not equal");
            }

            if (!name.Value.Equals(expectedName.Value))
            {
                throw new("not equal");
            }

            return octets;
        }

        public static FourCC ReadIntraChunkMarker(IOctetReader reader)
        {
            return ReadFourCC(reader);
        }
    }
}