/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Piot.Flood;

namespace Piot.OrderedDatagrams
{
    public static class OrderedDatagramsSequenceIdWriter
    {
        public static void Write(IOctetWriter writer, OrderedDatagramsSequenceId datagramsOut)
        {
            writer.WriteUInt16(datagramsOut.Value);
        }
    }
}