/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/monotonic-time-lb-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Piot.Flood;

namespace Piot.MonotonicTimeLowerBits
{
    public static class MonotonicTimeLowerBitsReader
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MonotonicTimeLowerBits Read(IOctetReader reader)
        {
#if DEBUG
            OctetMarker.AssertMarker(reader, Constants.MonotonicTimeLowerBitsSync);
#endif
            return new(reader.ReadUInt16());
        }
    }
}