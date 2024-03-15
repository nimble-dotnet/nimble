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
    public static class StatusWriter
    {
        /// <summary>
        ///     Sent from client to host. Client describes the last authoritative tickId it has received
        ///     and how many delta ticks it has detected as dropped after that.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="lastReceivedTickId"></param>
        /// <param name="droppedFramesAfterThat"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(OctetWriter writer, TickId lastReceivedTickId, byte droppedFramesAfterThat)
        {
#if DEBUG
            //writer.WriteUInt8(Constants.SnapshotReceiveStatusSync);
#endif

            TickIdWriter.Write(writer, lastReceivedTickId);
            writer.WriteUInt8(droppedFramesAfterThat);
        }
    }
}