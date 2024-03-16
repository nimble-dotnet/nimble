/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Piot.FixedStruct;
using Piot.Flood;

namespace Piot.Replay.Serialization
{
    public static class FixedOctets32Reader
    {
        public static FixedOctets32WithLength Read(IOctetReader reader)
        {
            var length = reader.ReadUInt8();
            if (length > 32)
            {
                throw new Exception($"application version has wrong length {length}");
            }

            var fixedOctets = new FixedOctets32WithLength();
            
            fixedOctets.CopyFrom(reader.ReadOctets(length));

            return fixedOctets;
        }
    }
}