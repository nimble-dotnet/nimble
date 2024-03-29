/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Text;
using Piot.Flood;

namespace Piot.SerializableVersion.Serialization
{
    public static class VersionWriter
    {
        public static void Write(OctetWriter writer, SemanticVersion semanticVersion)
        {
            writer.WriteUInt16(semanticVersion.major);
            writer.WriteUInt16(semanticVersion.minor);
            writer.WriteUInt16(semanticVersion.patch);
            writer.WriteUInt8((byte)semanticVersion.suffix.Length);
            if (semanticVersion.suffix.Length > 0)
            {
                writer.WriteOctets(Encoding.ASCII.GetBytes(semanticVersion.suffix));
            }
        }
    }
}