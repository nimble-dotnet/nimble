/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Runtime.CompilerServices;
using Piot.MonotonicTime;

namespace Piot.Stats
{
    public sealed class StatPerSecond
    {
        readonly FixedDeltaTimeMs minimumAverageTime;
        uint averageCount;
        uint count;
        bool isInitialized;
        TimeMs lastTime;
        int max;
        int min;
        Stat stat;
        long total;

        public StatPerSecond(TimeMs now, FixedDeltaTimeMs minimumAverageTime)
        {
            Reset(now);

            this.minimumAverageTime = minimumAverageTime;
        }

        public Stat Stat => stat;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(int a)
        {
            total += a;
            count++; // count is technically not needed, but is nice to know how many samples the average was based on.
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Reset(TimeMs now)
        {
            count = 0;
            total = 0;
            lastTime = now;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(TimeMs now)
        {
            if (isInitialized && now.ms < lastTime.ms + minimumAverageTime.ms)
            {
                return;
            }

            var deltaMilliseconds = now.ms - lastTime.ms;
            if (deltaMilliseconds == 0)
            {
                return;
            }

            var a = (int)(total * 1000 / deltaMilliseconds);
            if (a < min)
            {
                min = a;
            }

            if (a > max)
            {
                max = a;
            }

            stat.average = a;
            stat.min = min;
            stat.max = max;
            stat.count = count;

            averageCount++;
            if (averageCount > 5)
            {
                min = int.MaxValue;
                max = int.MinValue;
                averageCount = 0;
                isInitialized = true;
            }

            Reset(now);
        }
    }
}