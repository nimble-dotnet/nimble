/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Piot.Clog;
using Piot.Tick;


namespace Piot.Nimble.Steps
{
    internal static class CompareOctets
    {
        internal static bool Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
        {
            return a.SequenceEqual(b);
        }
    }


    public class PredictedStepsLocalPlayers
    {
        public Dictionary<byte, PredictedStepsQueue> predictedStepsQueues = new();

        private ILog log;

        public PredictedStepsLocalPlayers(ILog log)
        {
            this.log = log;
        }

        public PredictedStepsQueue GetStepsQueueForLocalPlayer(LocalPlayerIndex playerIndex)
        {
            var queue = predictedStepsQueues[playerIndex.Value];
           // log.DebugLowLevel("get queue for {LocalPlayerIndex} {queue}", playerIndex, queue);
            return queue;
        }

        public bool TryGetStepsQueueForLocalPlayer(LocalPlayerIndex localPlayerIndex, out PredictedStepsQueue queue)
        {
            return predictedStepsQueues.TryGetValue(localPlayerIndex.Value, out  queue);
        }

        public PredictedStepsQueue CreateLocalPlayer(LocalPlayerIndex playerIndex, TickId waitingForTickId)
        {
            log.DebugLowLevel("create local player {LocalPlayerIndex}", playerIndex);
            var queue = new PredictedStepsQueue(waitingForTickId);
            predictedStepsQueues.Add(playerIndex.Value, queue);

            return queue;
        }

        public void DiscardUpToAndExcluding(TickId tickIdNext)
        {
            log.DebugLowLevel("discard up to and excluding {TickId}", tickIdNext);
            foreach (var (_, queue) in predictedStepsQueues)
            {
                queue.DiscardUpToAndExcluding(tickIdNext);
            }
        }

        public TickIdRange Range()
        {
            var range = new TickIdRange();

            foreach (var (_, queue) in predictedStepsQueues)
            {
                if (queue.IsEmpty)
                {
                    continue;
                }

                // TODO:
                range = queue.Range;
            }

            return range;
        }
    }


    /// <summary>
    ///     Serialized Game specific input step in the <see cref="PredictedStep.payload" />.
    /// </summary>
    public readonly struct PredictedStep
    {
        public readonly TickId appliedAtTickId;
        public readonly ReadOnlyMemory<byte> payload;

        public PredictedStep(TickId appliedAtTickId, ReadOnlySpan<byte> payload)
        {
            this.appliedAtTickId = appliedAtTickId;
            this.payload = payload.ToArray();
        }

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            var other = (PredictedStep)obj;

            return other.appliedAtTickId.tickId == appliedAtTickId.tickId &&
                   CompareOctets.Compare(other.payload.Span, payload.Span);
        }

        public override string ToString()
        {
            byte debugByte = 0;

            if (payload.Length > 9)
            {
                debugByte = payload.Span[9];
            }

            return
                $"[PredictedStep TickId:{appliedAtTickId} octetSize:{payload.Length} debug:{debugByte}]";
        }
    }
}