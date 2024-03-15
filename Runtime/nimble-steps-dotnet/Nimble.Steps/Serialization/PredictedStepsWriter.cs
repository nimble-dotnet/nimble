/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Piot.Nimble.Steps.Serialization
{
    public static class Constants
    {
        public const byte PredictedStepsHeaderMarker = 0xdb;
        public const byte PredictedStepsPayloadHeaderMarker = 0xdc;
    }

    public static class PredictedStepsWriter
    {
        /// <summary>
        ///     Serializing the game specific inputs to be sent from the client to the authoritative host.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickId Write(OctetWriter writer, PredictedStepsLocalPlayers inputsForLocalPlayers,
            ILog log)
        {
            OctetMarker.WriteMarker(writer, Constants.PredictedStepsHeaderMarker);
            var localPlayerCount = inputsForLocalPlayers.predictedStepsQueues.Count;
            writer.WriteUInt8((byte)localPlayerCount);

            TickId highestWrittenPredictedTickId = default;

            if (localPlayerCount == 0)
            {
                return highestWrittenPredictedTickId;
            }

            var octetBudget = writer.OctetsLeft / localPlayerCount;
            var totalEndPosition = writer.Size - 10;

            log.DebugLowLevel("{OctetBudget} for each participant {LocalPlayerCount}", octetBudget, localPlayerCount);

            foreach (var (localPlayerIndex, predictedStepsQueue) in inputsForLocalPlayers.predictedStepsQueues)
            {
                var endPosition = writer.Position + octetBudget;
                if (endPosition >= totalEndPosition)
                {
                    endPosition = totalEndPosition;
                }

                writer.WriteUInt8(localPlayerIndex);
                var positionBeforeTickCount = writer.Tell;
                writer.WriteUInt8(0);

                var tickCount = predictedStepsQueue.Count;
                if (tickCount == 0)
                {
                    //log.Notice("no predicted steps to send");
                    continue;
                }

                if (tickCount > 255)
                {
                    throw new("too many inputs to serialize");
                }


                var first = predictedStepsQueue.Peek();
                TickIdWriter.Write(writer, first.appliedAtTickId);
//				log.Notice("first {TickCount} {LocalPlayerID}  {TickID}", tickCount,
//					stepsForPlayer.localPlayerIndex, first.appliedAtTickId);
                var expectedTickIdValue = first.appliedAtTickId.tickId;

                var wroteTickCount = 0;
                foreach (var predictedStep in predictedStepsQueue.Collection)
                {
                    if (predictedStep.appliedAtTickId.tickId != expectedTickIdValue)
                    {
                        throw new(
                            $"predicted step in wrong order in collection. Expected {expectedTickIdValue} but received {predictedStep.appliedAtTickId.tickId}");
                    }

                    OctetMarker.WriteMarker(writer, Constants.PredictedStepsPayloadHeaderMarker);

                    if (writer.Position + predictedStep.payload.Length + 1 + 4 >= endPosition)
                    {
                        break;
                    }

//					log.Debug("writing predicted step {{TickID}} {{PayloadLength}}", predictedStep.appliedAtTickId,
//						(byte)predictedStep.payload.Length);
                    writer.WriteUInt8((byte)predictedStep.payload.Length);
                    writer.WriteOctets(predictedStep.payload.Span);

                    if (predictedStep.appliedAtTickId > highestWrittenPredictedTickId)
                    {
                        highestWrittenPredictedTickId = predictedStep.appliedAtTickId;
                    }

                    wroteTickCount++;
                    expectedTickIdValue++;
                }

                log.DebugLowLevel("Wrote predicted {FirstTickID} {StepCount} to datagram for {LocalPlayerIndex}", first,
                    wroteTickCount, localPlayerIndex);
                var seekBack = writer.Tell;
                writer.Seek(positionBeforeTickCount);
                writer.WriteUInt8((byte)wroteTickCount);
                writer.Seek(seekBack);
            }

            return highestWrittenPredictedTickId;
        }
    }
}