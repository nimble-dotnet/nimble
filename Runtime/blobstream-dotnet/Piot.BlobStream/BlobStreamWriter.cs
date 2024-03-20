/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using Piot.Clog;
using Piot.MonotonicTime;

namespace Piot.BlobStream
{
    public struct BlobStreamWriterEntry
    {
        /// <summary>
        /// Gets or sets the chunk's octet data.
        /// </summary>
        public int index;

        /// <summary>
        /// Gets or sets the count of octets in this chunk.
        /// </summary>
        public ulong octetCount;

        /// <summary>
        /// Gets or sets the chunk ID.
        /// </summary>
        public ulong chunkId;

        /// <summary>
        /// Gets or sets the last time this chunk was sent. Represented as a <see cref="TimeMs"/>.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public TimeMs lastSentAtTime;

        /// <summary>
        /// Gets or sets the count of times this chunk has been sent.
        /// </summary>
        public ulong sendCount;

        /// <summary>
        /// Gets or sets a value indicating whether this chunk has been received by the remote endpoint.
        /// </summary>
        public bool isReceived;
    }


    /// <summary>
    /// Manages the streaming of a blob by dividing it into chunks and handling acknowledgments
    /// for chunks that have been successfully received. This allows for efficient transmission and
    /// retransmission of data over transports where packet loss may occur.
    /// </summary>
    public class BlobStreamWriter
    {
        private readonly ILog log;
        private readonly byte[] blob;
        private readonly ulong octetCount;
        private readonly ulong fixedChunkSize;
        private bool isComplete;
        private readonly BlobStreamWriterEntry[] entries;
        
        /// <summary>
        /// Gets the total count of octets in the blob.
        /// </summary>
        public ulong OctetCount => octetCount;

        /// <summary>
        /// Indicates whether the entire blob has been acknowledged as received.
        /// </summary>
        public bool IsComplete => isComplete;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamWriter"/> class for managing
        /// the transmission of a blob.
        /// </summary>
        /// <param name="data">The blob data to be transmitted.</param>
        /// <param name="fixedChunkSize">The size of each chunk into which the blob is divided for transmission. Maximum allowed size is 1024.</param>
        /// <param name="now">The current time, used for initializing transmission timing information.</param>
        /// <param name="log">The logger for recording events and errors.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> or <paramref name="log"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="fixedChunkSize"/> is greater than 1024.</exception>
        public BlobStreamWriter(byte[] data, ulong fixedChunkSize, TimeMs now, ILog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.blob = data ?? throw new ArgumentNullException(nameof(data));
            this.octetCount = (ulong)data.Length;
            this.fixedChunkSize = fixedChunkSize;
            if (fixedChunkSize > 1024)
            {
                throw new ArgumentException("Only chunks up to 1024 are supported", nameof(fixedChunkSize));
            }

            isComplete = false;
            var chunkCount = (octetCount + fixedChunkSize - 1) / fixedChunkSize;
            entries = new BlobStreamWriterEntry[chunkCount];
            InitializeEntries(chunkCount, now);
        }

        private void InitializeEntries(ulong chunkCount, TimeMs now)
        {
            for (ulong i = 0; i < chunkCount; ++i)
            {
                var expectedChunkSize = i == chunkCount - 1
                    ? (octetCount % fixedChunkSize == 0 ? fixedChunkSize : octetCount % fixedChunkSize)
                    : fixedChunkSize;

                entries[i].octetCount = expectedChunkSize;
                entries[i].index = (int)(i * fixedChunkSize);
                entries[i].chunkId = i;
                entries[i].lastSentAtTime = now;
                entries[i].sendCount = 0;
                entries[i].isReceived = false;
            }
        }

        /// <summary>
        /// Provides a read-only span of the specified chunk from the blob.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk to retrieve.</param>
        /// <returns>A <see cref="ReadOnlySpan{Byte}"/> containing the bytes of the specified chunk.</returns>

        public ReadOnlySpan<byte> Span(uint chunkIndex)
        {
            return blob.AsSpan((int)(chunkIndex * fixedChunkSize), (int)entries[chunkIndex].octetCount);
        }

        /// <summary>
        /// Marks chunks as received based on the provided acknowledgment information.
        /// </summary>
        /// <param name="everythingBeforeThis">Specifies that all chunks before this index have been received.</param>
        /// <param name="maskReceived">A bitmask indicating the receipt of chunks starting from the index specified by <paramref name="everythingBeforeThis"/>.</param>
        public void MarkReceived(ushort everythingBeforeThis, ulong maskReceived)
        {
            if (everythingBeforeThis > entries.Length)
            {
                log.Error("Strange everythingBeforeThis");
                return;
            }

            log.DebugLowLevel("MarkReceived remote expecting {EverythingBeforeThis} mask {MaskReceived}",
                everythingBeforeThis, maskReceived);

            if (isComplete)
            {
                return;
            }

            for (var i = 0; i < everythingBeforeThis; ++i)
            {
                entries[i].isReceived = true;
            }

            if (everythingBeforeThis == entries.Length)
            {
                isComplete = true;
                log.DebugLowLevel("Remote has received everything");
                return;
            }

            for (var i = 0; i < 64; ++i)
            {
                var index = everythingBeforeThis + i;
                if (index >= entries.Length)
                {
                    break;
                }

                if ((maskReceived & (1UL << i)) == 0)
                {
                    continue;
                }

                entries[index].isReceived = true;
                log.DebugLowLevel("Remote has received {ChunkId}", index);
            }
        }

        /// <summary>
        /// Determines which chunks need to be sent or resent based on reception acknowledgments and timing.
        /// </summary>
        /// <param name="now">The current time, used to determine retransmission requirements.</param>
        /// <param name="maxEntriesCount">The maximum number of chunk indices to return, limiting the number of chunks sent in one update cycle.</param>
        /// <returns>A collection of chunk indices indicating which chunks need to be sent.</returns>
        public IEnumerable<uint> GetChunksToSend(TimeMs now, uint maxEntriesCount)
        {
            var result = new List<uint>();

            for (var i = 0; i < entries.Length; ++i)
            {
                var entry = entries[i];
                if (entry.isReceived)
                {
                    continue;
                }

                if (result.Count >= maxEntriesCount)
                {
                    break;
                }

                var timeSinceLastSent = now.ms - entry.lastSentAtTime.ms;
                if (entry.sendCount != 0 && timeSinceLastSent <= 200)
                {
                    continue;
                }

                entries[i].lastSentAtTime = now;
                entries[i].sendCount++;

                result.Add((uint)i);

                log.DebugLowLevel("Sending {ChunkId}", entry.chunkId);
            }

            return result;
        }
    }
}