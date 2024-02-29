/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/flood-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Flood
{
    public interface IOctetWriterResult
    {
        public ReadOnlySpan<byte> Octets { get; }
    }
}