/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;

namespace Piot.Stats
{
    public struct StatPerSecondWithFormatter
    {
        Func<int, string> formatter;
        private Stat stat;

        public Func<int, string> Formatter
        {
            set => formatter = value ?? throw new ArgumentException("wrong argument");
        }

        public StatPerSecondWithFormatter(Func<int, string> formatter, Stat stat)
        {
            this.formatter = formatter;
            this.stat = stat;
        }

        public override string ToString()
        {
            return $"[{formatter(stat.average)} min:{formatter(stat.min)}, max:{formatter(stat.max)}]";
        }
    }

    public struct Stat
    {
        public int average;
        public uint count;
        public int min;
        public int max;
    }
}