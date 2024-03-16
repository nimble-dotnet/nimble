/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Piot.Flood;

namespace Piot.Replay.Serialization
{
    public static class ApplicationVersionWriter
    {
        public static void Write(IOctetWriter writer, ApplicationVersion applicationVersion)
        {
            writer.WriteUInt8((byte)applicationVersion.Length);
            writer.WriteOctets(applicationVersion.a.a01.ToArray());
        }
    }
}