/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Piot.Flood;

namespace Piot.OrderedDatagrams
{
    public static class OrderedDatagramsSequenceIdReader
    {
        public static OrderedDatagramsSequenceId Read(IOctetReader reader)
        {
            var encounteredId = reader.ReadUInt16();

            return new(encounteredId);
        }
    }
}