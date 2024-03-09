/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/monotonic-time-lb-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using Piot.MonotonicTime;

namespace Piot.MonotonicTimeLowerBits
{
    public static class LowerBitsToMonotonic
    {
        public static TimeMs LowerBitsToMonotonicMs(TimeMs now, MonotonicTimeLowerBits lowerBits)
        {
            var nowBits = (ulong)(now.ms & 0xffff);
            var receivedLowerBits = (ulong)lowerBits.lowerBits;
            var top = (ulong)now.ms & 0xffffffffffff0000;

            var receivedMonotonic = top | receivedLowerBits;
            if (receivedLowerBits > nowBits)
            {
                receivedMonotonic -= 0x10000;
            }

            var diff = (ulong)now.ms - receivedMonotonic;
            if (diff > 16000)
            {
                throw new($"suspicious time lower bits diff {diff}, was application paused?");
            }

            return new((long)receivedMonotonic);
        }

        public static TimeMs LowerBitsToPastMonotonicMs(TimeMs now, MonotonicTimeLowerBits lowerBits)
        {
            var nowBits = (ulong)(now.ms & 0xffff);
            var receivedLowerBits = (ulong)lowerBits.lowerBits;
            var top = (ulong)now.ms & 0xffffffffffff0000;

            var receivedMonotonic = top | receivedLowerBits;
            if (receivedLowerBits < nowBits)
            {
                receivedMonotonic -= 0x10000;
            }

            var diff = receivedMonotonic - (ulong)now.ms;
            if (diff > 300200)
            {
//                throw new Exception($"suspicious time lower bits diff {diff}");
            }

            return new((long)receivedMonotonic);
        }
    }
}