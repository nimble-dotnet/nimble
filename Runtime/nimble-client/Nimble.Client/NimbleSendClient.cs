using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.Nimble.Steps.Serialization;
using Constants = Piot.Datagram.Constants;

namespace Piot.Nimble.Client
{
    public sealed class NimbleSendClient
    {
        public const uint MaxOctetSize = 1024;
        public PredictedStepsLocalPlayers predictedSteps;
        private ILog log;
        private OctetWriter octetWriter = new(1024);
        private const int MaximumClientOutDatagramCount = 4;
        private CircularBuffer<ClientDatagram> clientOutDatagrams = new(MaximumClientOutDatagramCount);

        public NimbleSendClient(ILog log)
        {
            this.log = log;
            predictedSteps = new PredictedStepsLocalPlayers();
        }

        public IEnumerable<ClientDatagram> OutDatagrams => clientOutDatagrams;


        public void Tick()
        {
            var filteredOutPredictedStepsForLocalPlayers = FilterOutStepsToSend();

            octetWriter.Reset();
            PredictedStepsSerialize.Serialize(octetWriter, filteredOutPredictedStepsForLocalPlayers, log);


//			log.Warn($"decision to send predicted steps to send to the host {filteredOutPredictedStepsForLocalPlayers} {{OctetCount}}", octetWriter.Position);

            clientOutDatagrams.Clear();

            if (octetWriter.Position > Constants.MaxDatagramOctetSize)
            {
                throw new Exception($"too many predicted steps to serialize");
            }

            ref var datagram = ref clientOutDatagrams.EnqueueRef();
            datagram.payload = octetWriter.Octets.ToArray();
        }

        private PredictedStepsForAllLocalPlayers FilterOutStepsToSend()
        {
            var predictedStepsForPlayers = new List<PredictedStepsForPlayer>();
            var localPlayerCount = predictedSteps.predictedStepsQueues.Count;
            if (localPlayerCount == 0)
            {
                return default;
            }

            var maxOctetSizePerPlayer = MaxOctetSize / localPlayerCount;
            foreach (var (playerIndex, predictedStepsQueue) in predictedSteps.predictedStepsQueues)
            {
                // HACK, Make sure predicted queues aren't too big
                if (predictedStepsQueue.Count > 25)
                {
                    var diff = predictedStepsQueue.Count - 25;
                    predictedStepsQueue.DiscardUpToAndExcluding(new(
                        (uint)(predictedStepsQueue.Peek().appliedAtTickId.tickId +
                               diff)));
                }

                if (predictedStepsQueue.IsEmpty)
                {
                    continue;
                }

                var allPredictedSteps = predictedStepsQueue.Collection;

                //		log.Debug("prepare predictedStep for {{PlayerIndex}}", playerIndex);
                
                var octetCount = 0;
                var stepCount = 0;
                foreach (var predictedStep in allPredictedSteps)
                {
//					log.Debug($"prepare predictedStep: {{PlayerIndex}} {{TickID}}", playerIndex,
//						predictedStep.appliedAtTickId);
                    octetCount += predictedStep.payload.Length + 2;
                    if (octetCount > maxOctetSizePerPlayer)
                    {
                        log.Debug("we reached our limit, break here {OctetCount} {MaxOctetSizePerPlayer}", octetCount,
                            maxOctetSizePerPlayer);
                        break;
                    }

                    stepCount++;
                }

                if (stepCount == 0)
                {
                    log.Notice("didnt have room to add a single step into the buffer {MaxOctetSizePerPlayer}",
                        maxOctetSizePerPlayer);
                }


                var filteredOutSteps = allPredictedSteps.Take(stepCount);

                var predictedStepsForOnePlayer = new PredictedStepsForPlayer(playerIndex, filteredOutSteps.ToArray());
                predictedStepsForPlayers.Add(predictedStepsForOnePlayer);
            }

            var allPlayers = new PredictedStepsForAllLocalPlayers(predictedStepsForPlayers.ToArray());

            return allPlayers;
        }
    }
}