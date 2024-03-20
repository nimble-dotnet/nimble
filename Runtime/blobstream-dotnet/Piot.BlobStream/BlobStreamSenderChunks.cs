using System;

namespace Piot.BlobStream
{
    public struct BlobStreamSendChunk
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
    }

    public class BlobStreamSenderChunks
    {
        private BlobStreamSendChunk[] entries;
        private readonly byte[] blob;
        private readonly ulong octetCount;
        private readonly ulong fixedChunkSize;
        private readonly uint chunkCount;
        
        public BlobStreamSenderChunks(ReadOnlySpan<byte> octets, ulong fixedChunkSize)
        {
            this.blob = octets.ToArray();
            this.octetCount = (ulong)octets.Length;
            this.fixedChunkSize = fixedChunkSize;
            if (fixedChunkSize > 1024)
            {
                throw new ArgumentException("Only chunks up to 1024 are supported", nameof(fixedChunkSize));
            }

            chunkCount = (uint)((octetCount + fixedChunkSize - 1) / fixedChunkSize);
            InitializeEntries();
        }

        public uint ChunkCount => chunkCount;
        public uint OctetCount => (uint)octetCount;

        private void InitializeEntries()
        {
            for (ulong i = 0; i < chunkCount; ++i)
            {
                var expectedChunkSize = i == chunkCount - 1
                    ? (octetCount % fixedChunkSize == 0 ? fixedChunkSize : octetCount % fixedChunkSize)
                    : fixedChunkSize;

                entries[i].octetCount = expectedChunkSize;
                entries[i].index = (int)(i * fixedChunkSize);
                entries[i].chunkId = i;
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
    }
}