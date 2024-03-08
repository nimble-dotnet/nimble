/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Piot.Discoid;
using Piot.Tick;
using UnityEngine;

namespace Nimble.Authoritative.Steps
{
    /// <summary>
    /// </summary>
    public sealed class CombinedAuthoritativeStepsQueue
    {
        readonly CircularBuffer<CombinedAuthoritativeStep> queue = new(60);

        TickId waitingForTickId;

        public IEnumerable<CombinedAuthoritativeStep> Collection => queue;

        public int Count => queue.Count;

        public int Capacity => queue.Capacity;

        public bool IsEmpty => Count == 0;

        public TickId WaitingForTickId => waitingForTickId;

        public CombinedAuthoritativeStep Last => queue.Last;

        public TickIdRange Range => new(queue.Peek().appliedAtTickId, queue.Last.appliedAtTickId);

        public CombinedAuthoritativeStep Peek()
        {
            return queue.Peek();
        }

        public CombinedAuthoritativeStepsQueue(TickId tickId)
        {
            waitingForTickId = tickId;
        }

        public void Add(CombinedAuthoritativeStep authoritativeStep)
        {
            if (authoritativeStep.appliedAtTickId != waitingForTickId)
            {
                throw new Exception(
                    $"authoritative steps can not have gaps. waiting for {waitingForTickId}, but received {authoritativeStep.appliedAtTickId}");
            }

            queue.Enqueue(authoritativeStep);
            waitingForTickId = new(authoritativeStep.appliedAtTickId.tickId + 1);
        }

        public void Reset()
        {
            queue.Clear();
        }

        /*
        public bool HasInputForTickId(TickId tickId)
        {
            if (queue.Count == 0)
            {
                return false;
            }

            var firstTickId = queue.Peek().appliedAtTickId.tickId;
            var lastTick = waitingForTickId.tickId - 1;

            return tickId.tickId >= firstTickId && tickId.tickId <= lastTick;
        }

        public CombinedAuthoritativeStep GetInputFromTickId(TickId tickId)
        {
            foreach (var input in queue)
            {
                if (input.appliedAtTickId == tickId)
                {
                    return input;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(tickId), "tick id is not found in queue");
        }
        */

        public void DiscardUpToAndExcluding(TickId tickId)
        {
            while (queue.Count > 0)
            {
                if (queue.Peek().appliedAtTickId.tickId < tickId.tickId)
                {
                    queue.Discard(1);
                }
                else
                {
                    break;
                }
            }
        }

        public CombinedAuthoritativeStep Dequeue()
        {
            return queue.Dequeue();
        }

        public IEnumerable<CombinedAuthoritativeStep> FromRange(TickIdRange range)
        {
            if (queue.IsEmpty)
            {
                return default;
            }

            var startTickId = queue.Peek().appliedAtTickId.tickId;
            var startOffset = range.startTickId.tickId - startTickId;
            if (startOffset >= queue.Count)
            {
                throw new Exception($"startOffset is too big");
            }

            var endOffset = range.lastTickId.tickId - startTickId;
            if (endOffset >= queue.Count)
            {
                throw new Exception($"startOffset is too big");
            }

            var count = endOffset - startOffset + 1;

            var enumerator = queue.GetRangeEnumerator(startOffset, count);
            return new EnumeratorWrapper<CombinedAuthoritativeStep>(enumerator);
        }
    }
}