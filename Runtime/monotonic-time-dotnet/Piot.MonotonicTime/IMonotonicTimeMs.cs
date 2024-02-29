/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/monotonic-time-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

namespace Piot.MonotonicTime
{
    public interface IMonotonicTimeMs
    {
        TimeMs TimeInMs { get; }
    }
}