/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/flood-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

namespace Piot.Flood
{
    public static class BitReaderUtils
    {
        public static int ReadSignedBits(IBitReader reader, int bitCount)
        {
            return (int)reader.ReadBits(bitCount) - (1 << bitCount - 1);
        }
    }
}