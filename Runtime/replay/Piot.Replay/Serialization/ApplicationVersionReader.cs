/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using Piot.Flood;

namespace Piot.Replay.Serialization
{
    public static class ApplicationVersionReader
    {
        public static ApplicationVersion Read(IOctetReader reader)
        {
            var length = reader.ReadUInt8();
            if (length > 32)
            {
                throw new Exception($"application version has wrong length {length}");
            }

            return new ApplicationVersion(reader.ReadOctets(length));
        }
    }
}