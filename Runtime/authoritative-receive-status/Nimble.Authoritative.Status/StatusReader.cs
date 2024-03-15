/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Piot.Nimble.AuthoritativeReceiveStatus
{
    public static class StatusReader
    {
        /// <summary>
        ///     Read on host coming from client.
        ///     Client describes the last authoritative TickId it has received and how many tickIds it has
        ///     detected as dropped after that.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="expectingTickId"></param>
        /// <param name="droppedTicksAfterThat"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Read(IOctetReader reader, out TickId expectingTickId,
            out byte droppedTicksAfterThat)
        {
#if DEBUG
           // if (reader.ReadUInt8() != Constants.SnapshotReceiveStatusSync)
            //{
             //   throw new("desync");
             // }
#endif
            expectingTickId = TickIdReader.Read(reader);
            droppedTicksAfterThat = reader.ReadUInt8();
        }
    }
}