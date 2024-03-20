/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Piot.Clog;

namespace Piot.BlobStream
{
    /// <summary>
    /// Reads and assembles chunks of a blob into a complete octet array.
    /// </summary>
    public class BlobStreamReceiver
    {
        private ILog log;
        private byte[] blob;
        private ulong octetCount;
        private ulong fixedChunkSize;
        private bool isComplete;
        private BitArray bitArray;

        /// <summary>
        /// Gets the total number of octets that the blob stream is expected to contain.
        /// </summary>
        public ulong OctetCount => octetCount;

        public ulong FixedChunkSize => fixedChunkSize;
        public bool IsComplete => isComplete;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamReceiver"/> class.
        /// </summary>
        /// <param name="octetCount">The total size of the blob in octets.</param>
        /// <param name="fixedChunkSize">The size of each chunk in octets. The last chunk can be smaller.</param>
        /// <param name="log">The logging interface used for error and debug logging.</param>
        public BlobStreamReceiver(ulong octetCount, ulong fixedChunkSize, ILog log)
        {
            this.log = log;
            this.octetCount = octetCount;
            blob = new byte[octetCount];
            var chunkCount = (octetCount + fixedChunkSize - 1) / fixedChunkSize;
            this.fixedChunkSize = fixedChunkSize;
            bitArray = new BitArray((int)chunkCount);
        }

        /// <summary>
        /// Sets the chunk of data at the specified chunk ID.
        /// </summary>
        /// <param name="chunkId">The ID (zero-based index) of the chunk being set.</param>
        /// <param name="octets">The byte array containing the chunk's data.</param>
        /// <remarks>
        /// This method updates the internal state of the blob stream reader,
        /// marking chunks as received and checking if the entire stream is complete.
        /// </remarks>
        public void SetChunk(ushort chunkId, ReadOnlySpan<byte> octets)
        {
            var chunkOctetCount = (ulong)octets.Length;
            var offset = chunkId * fixedChunkSize;
            if (offset + chunkOctetCount > octetCount)
            {
                log.Error("blobStreamInSetChunk overwrite");
            }

            if (chunkId == (ushort)(bitArray.Length - 1))
            {
                var expectedLastChunkSize = OctetCount % fixedChunkSize;
                if (expectedLastChunkSize == 0)
                {
                    expectedLastChunkSize = fixedChunkSize;
                }

                if (chunkOctetCount != expectedLastChunkSize)
                {
                    log.Error("last chunk size must exactly. {octetCount} vs {expectedLastChunkSize}", octetCount,
                        expectedLastChunkSize);
                }
            }
            else
            {
                if (chunkOctetCount != fixedChunkSize)
                {
                    log.Error("chunk size must be equal to fixed chunk size. {octetCount} vs {_fixedChunkSize}",
                        octetCount, fixedChunkSize);
                }
            }

            Buffer.BlockCopy(octets.ToArray(), 0, blob, (int)offset, (int)octetCount);

            bitArray.Set(chunkId);

            if (!CheckAllBitsSet())
            {
                return;
            }
            
            log.DebugLowLevel("stream is complete");
            
            isComplete = true;
        }

        /// <summary>
        /// Checks if all bits in the internal bit array are set, indicating all chunks have been received.
        /// </summary>
        /// <returns><c>true</c> if all chunks have been received; otherwise, <c>false</c>.</returns>
        private bool CheckAllBitsSet()
        {
            return bitArray.AreAllSet();
        }

        /// <summary>
        /// Finds the index of the first chunk that is not received.
        /// </summary>
        /// <returns>The index of the chunk that is not received, or the chunk count if all chunks have been received.</returns>
        public uint FirstUnsetChunkIndex()
        {
            return bitArray.FirstUnset();
        }

        /// <summary>
        /// Retrieves a subset of chunk indices that has not been received, starting from the specified chunk index.
        /// </summary>
        /// <param name="fromIndex">The zero-based starting chunk index from which to receive the chunk info.</param>
        /// <returns>
        /// A <see cref="ulong"/> representing the subset of chunks received, 
        /// starting at the specified index. Chunk indices are packed into the <see cref="ulong"/> from left to right,
        /// meaning that the first chunk index bit received is the most significant bit of the result.
        /// </returns>
        public ulong GetBitsStartingFrom(uint index)
        {
            return bitArray.GetBitsStartingFrom(index);
        }

        public override string ToString()
        {
            return $"[BlobStreamRead {bitArray}]";
        }
    }
}