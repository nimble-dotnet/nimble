/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Piot.Clog;
using Piot.Flood;
using Piot.Nimble.Steps;
using Piot.Nimble.Steps.Serialization;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Nimble.Authoritative.Steps
{
    public static class PredictedStepsDeserialize
    {
        public const byte MaxPredictedStepOctetCount = 64;

        /// <summary>
        ///     Deserializes game specific steps arriving on the host from the client.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TickId Deserialize(IOctetReader reader, byte connectionId,
            ConnectionToParticipants connectionToParticipants, Participants participants, ILog log)
        {
            TickId highestAcceptedTickId = default;

            OctetMarker.AssertMarker(reader, Constants.PredictedStepsHeaderMarker);
            var localPlayerCount = reader.ReadUInt8();
            if (localPlayerCount == 0)
            {
                //log.Notice("no predicted steps from client");
                return highestAcceptedTickId;
            }


            for (var localPlayerSectionIndex = 0; localPlayerSectionIndex < localPlayerCount; ++localPlayerSectionIndex)
            {
                var localPlayerIndex = reader.ReadUInt8();
                var stepCount = reader.ReadUInt8();

                if (stepCount == 0)
                {
                    continue;
                }

                var hasExistingParticipant =
                    connectionToParticipants.TryGetParticipantConnectionFromLocalPlayer(
                        new(localPlayerIndex), out var participant);
                if (!hasExistingParticipant)
                {
                    participant = participants.CreateParticipant(connectionId,
                        new(localPlayerIndex));

                    log.Info(
                        "detect new local player index, creating a new {Participant} for {Connection} and {LocalIndex}",
                        participant, connectionId, localPlayerIndex);
                    connectionToParticipants.Add(new(localPlayerIndex),
                        participant);
                }

                var incomingSteps = participant.incomingSteps;

                var firstTickId = TickIdReader.Read(reader);

                var addedStepCount = 0;
                for (var i = 0; i < stepCount; ++i)
                {
                    OctetMarker.AssertMarker(reader, Constants.PredictedStepsPayloadHeaderMarker);
                    var payloadOctetCount = reader.ReadUInt8();
                    if (payloadOctetCount > MaxPredictedStepOctetCount)
                    {
                        throw new($"suspicious step predicted step octet count {payloadOctetCount}");
                    }

//					log.Debug("deserialize predicted payload {PayloadOctetCount}", payloadOctetCount);

                    var predictedStep = new PredictedStep(new((uint)(firstTickId.tickId + i)),
                        reader.ReadOctets(payloadOctetCount));


                    if (predictedStep.appliedAtTickId < incomingSteps.WaitingForTickId)
                    {
                        //log.Warn("skipping {PredictedTickId} since waiting for {WaitingForTickId}",
                        //  predictedStep.appliedAtTickId, participant.incomingSteps.WaitingForTickId);
                        continue;
                    }

//                    log.Warn("adding incoming predicted step {Participant} {PredictedStepTick}",
                    //                      participant, predictedStep.appliedAtTickId);

                    // TODO:
                    if (predictedStep.appliedAtTickId > highestAcceptedTickId)
                    {
                        highestAcceptedTickId = predictedStep.appliedAtTickId;
                    }

                    addedStepCount++;
                    var wasReset = incomingSteps.AddPredictedStep(predictedStep);
                    if (wasReset)
                    {
                        log.Notice("incoming queue was reset: {PredictedStep} for {Participant} {LocalPlayerIndex}",
                            predictedStep, participant, localPlayerIndex);
                    }
                }

                log.DebugLowLevel("Deserialized predicted steps {FirstTickID} {ReceivedTickCount} and {AcceptedTickCount}",
                    firstTickId, stepCount, addedStepCount);

                ///					log.Debug("deserialized predicted step {{LocalPlayerId}} {{PredictedStep}}", localPlayerId,
                //					predictedStep);
            }

            return highestAcceptedTickId;
        }
    }
}