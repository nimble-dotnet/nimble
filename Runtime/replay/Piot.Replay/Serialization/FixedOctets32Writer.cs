﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.FixedStruct;
using Piot.Flood;

namespace Piot.Replay.Serialization
{
    public static class FixedOctets32Writer
    {
        public static void Write(IOctetWriter writer, FixedOctets32WithLength applicationVersion)
        {
            writer.WriteUInt8((byte)applicationVersion.Length);
            writer.WriteOctets(applicationVersion.ToArray());
        }
    }
}