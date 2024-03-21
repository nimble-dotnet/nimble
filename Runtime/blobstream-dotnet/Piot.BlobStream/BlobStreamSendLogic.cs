/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections;
using System.Collections.Generic;
using Piot.Clog;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;

namespace Piot.BlobStream
{
    /// <summary>
    /// Manages the logic for writing to a blob stream, optimizing for network transmission by
    /// chunking data into optimal datagram sizes and managing acknowledgments for chunks received.
    /// </summary>
    public class BlobStreamSendLogic
    {
        private BlobStreamSender blobStream;
        private const int OptimalDatagramSize = 900;
        private readonly ILog log;
        private const int OptimalDatagramCountToSend = 4;
        private readonly OctetWriter cachedWriter = new(OptimalDatagramSize);

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamSendLogic"/> class.
        /// </summary>
        /// <param name="sender">The blob stream writer that holds the complete payload to be sent.</param>
        /// <param name="log">The logging interface for recording log messages.</param>
        public BlobStreamSendLogic(BlobStreamSender sender, ILog log)
        {
            this.log = log;
            this.blobStream = sender;
        }

        public bool IsReceivedByRemote => blobStream.IsComplete;

        public IEnumerable<uint> PrepareChunksToSend(TimeMs now)
        {
            return blobStream.GetChunksToSend(now, OptimalDatagramCountToSend);
        }

        /// <summary>
        /// Processes and prepares datagrams for sending, based on the current state of the blob stream
        /// and the elapsed time.
        /// </summary>
        public void WriteStream(OctetWriter writer, uint chunkIndex)
        {
            WriteEntry(writer, chunkIndex);
        }

        /// <summary>
        /// Processes received stream, handling commands such as acknowledgments for chunks.
        /// </summary>
        /// <param name="reader">The octet reader to read commands and data from.</param>
        /// <exception cref="Exception">Throws when an unknown command is encountered.</exception>
        public void ReceiveStream(IOctetReader reader)
        {
            var command = (ClientSendCommand)reader.ReadUInt8();
            switch (command)
            {
                case ClientSendCommand.AckChunk:
                    AckChunk(reader);
                    break;
                default:
                    throw new Exception($"unknown command {command}");
            }
        }

        private void WriteEntry(OctetWriter writer, uint chunkIndex)
        {
            if (writer.OctetsLeft < OptimalDatagramSize)
            {
                log.Notice(
                    "stream is too small, needed room for a {OptimalDatagramSize}, but has {OctetsLeftInStream}",
                    OptimalDatagramSize, writer.OctetsLeft);
                return;
            }

            WriteCommand(writer, ClientReceiveCommand.SetChunk);
            writer.WriteUInt32(chunkIndex);

            var payload = blobStream.Span(chunkIndex);
            log.DebugLowLevel("found chunk {ChunkIndex} {OctetLength}", chunkIndex, payload.Length);
            writer.WriteUInt16((ushort)payload.Length);
            writer.WriteOctets(payload);
        }

        private void WriteCommand(IOctetWriter writer, ClientReceiveCommand command)
        {
            writer.WriteUInt8((byte)command);
        }

        private void AckChunk(IOctetReader reader)
        {
            var waitingForChunkId = reader.ReadUInt32();
            var receiveMask = reader.ReadUInt64();

            log.DebugLowLevel($"ack chunk: {waitingForChunkId} mask:{receiveMask}");

            blobStream.MarkReceived((ushort)waitingForChunkId, receiveMask);
        }
    }
}