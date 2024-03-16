/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Piot.FixedStruct;

namespace Piot.Replay.Serialization
{
    public struct ApplicationVersion
    {
        public FixedOctets32WithLength a;

        public ApplicationVersion(ReadOnlySpan<byte> readOctets)
        {
            a = new();
            a.CopyFrom(readOctets);
        }

        public int Length => a.Length;
    }
}