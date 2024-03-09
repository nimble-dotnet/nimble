/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Stats
{
    public static class StandardFormatter
    {
        public static string Format(int value)
        {
            return $"{value}";
        }
    }

    public static class StandardFormatterPerSecond
    {
        public static string Format(int value)
        {
            return $"{value}/s";
        }
    }

    public static class BitFormatter
    {
        public static string Format(int value)
        {
            return value switch
            {
                < 1000 => $"{value} bit",
                < 1_000_000 => $"{value / 1000.0:0.0} kbit",
                < 1_000_000_000 => $"{value / 1_000_000.0:0.#} Mbit",
                _ => $"{value / 1_000_000_000:0.#} Gbit"
            };
        }
    }

    public static class BitsPerSecondFormatter
    {
        public static string Format(int value)
        {
            return $"{BitFormatter.Format(value)}/s";
        }
    }
}
