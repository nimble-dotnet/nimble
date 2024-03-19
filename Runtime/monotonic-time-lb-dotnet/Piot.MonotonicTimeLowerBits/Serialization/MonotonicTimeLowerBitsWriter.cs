/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/monotonic-time-lb-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Piot.Flood;

namespace Piot.MonotonicTimeLowerBits
{
    public static class Constants
    {
        public const byte MonotonicTimeLowerBitsSync = 0x64;
    }

    public static class MonotonicTimeLowerBitsWriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(MonotonicTimeLowerBits lowerBits, OctetWriter writer)
        {
            OctetMarker.WriteMarker(writer, Constants.MonotonicTimeLowerBitsSync);

            writer.WriteUInt16((ushort)(lowerBits.lowerBits & 0xffff));
        }
    }
}