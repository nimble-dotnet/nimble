/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Piot.Clog;
using Piot.Flood;

namespace Piot.BlobStream
{
    /// <summary>
    /// Handles the logic of receiving a blob over a transport that can have packet loss.
    /// </summary>
    public class BlobStreamReceiveLogic
    {
        private readonly BlobStreamReceiver blobStream;
        private ILog log;

        public BlobStreamReceiver BlobStream => blobStream;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStreamReceiveLogic"/> class.
        /// </summary>
        /// <param name="blobStream">The <see cref="BlobStreamReceiver"/> used to store and manage the blob's chunks.</param>
        /// <param name="log">The logging interface used for logging messages.</param>
        public BlobStreamReceiveLogic(BlobStreamReceiver blobStream, ILog log)
        {
            this.log = log;
            this.blobStream = blobStream;
        }

        /// <summary>
        /// Reads data from the specified <see cref="IOctetReader"/> stream and processes the commands found within.
        /// Currently, only the SetChunk command is supported, which updates the blob stream with new chunk data.
        /// </summary>
        /// <param name="inStream">The <see cref="IOctetReader"/> from which to read the data and commands.</param>
        /// <exception cref="Exception">Thrown when an illegal or unknown command is encountered.</exception>
        public void ReadStream(IOctetReader inStream)
        {
            var command = (ClientReceiveCommand) inStream.ReadUInt8();
            switch (command)
            {
                case ClientReceiveCommand.SetChunk:
                    ReadSetChunk(inStream);
                    break;
                default:
                    throw new Exception($"illegal command {command}");
            }
        }

        /// <summary>
        /// Writes acknowledgments for received chunks to the specified writer. This method
        /// encapsulates the logic for determining which chunks have been successfully received
        /// and communicates this information back to the sender, allowing for efficient data
        /// synchronization.
        /// </summary>
        /// <param name="writer">The <see cref="IOctetWriter"/> used for writing the acknowledgment data.</param>
        public void WriteStream(IOctetWriter writer)
        {
            WriteAck(writer);
        }
        
        /// <summary>
        /// Writes an acknowledgment to the specified <see cref="IOctetWriter"/> based on the chunks
        /// received and processed by the <see cref="BlobStreamReceiver"/>.
        /// </summary>
        /// <param name="writer">The <see cref="IOctetWriter"/> to which the acknowledgment is written.</param>
        private void WriteAck(IOctetWriter writer)
        {
            var waitingForChunkId = blobStream.FirstUnsetChunkIndex();
            var receiveMask = blobStream.GetBitsStartingFrom(waitingForChunkId + 1);

            Console.WriteLine($"blobStreamLogicIn: send. We are waiting for {waitingForChunkId:X4}, mask {receiveMask:X8}");
            WriteCommand(writer, ClientSendCommand.AckChunk);
            writer.WriteUInt32(waitingForChunkId);
            writer.WriteUInt64(receiveMask);
        }
        
        private void WriteCommand(IOctetWriter writer, ClientSendCommand command)
        {
            writer.WriteUInt8((byte) command);
        }

        /// <summary>
        /// Processes the SetChunk command, reading the chunk data from the stream and updating the blob stream.
        /// </summary>
        /// <param name="inStream">The stream from which to read the chunk data.</param>
        /// <exception cref="Exception">Thrown if the octet length of the chunk exceeds the fixed chunk size of the blob stream.</exception>
        private void ReadSetChunk(IOctetReader inStream)
        {
            var chunkId = inStream.ReadUInt32();
            var octetLength = inStream.ReadUInt16();

            log.DebugLowLevel("set Chunk {ChunkID} {OctetLength}", chunkId, octetLength);
            if (octetLength > blobStream.FixedChunkSize)
            {
                throw new Exception($"octetLength overrun {octetLength}");
            }

            blobStream.SetChunk((ushort)chunkId, inStream.ReadOctets(octetLength));
        }
    }
}