using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Datagram;
using Piot.Flood;
using Piot.Nimble.Steps.Serialization;
using Constants = Piot.Datagram.Constants;

namespace Piot.Nimble.Client
{
	public class NimbleSendClient
	{
		public const uint MaxOctetSize = 1024;
		public PredictedStepsLocalPlayers predictedSteps;
		private ILog log;
		private OctetWriter octetWriter = new(1024);
		private List<ClientDatagram> clientOutDatagrams = new();

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
			clientOutDatagrams.Clear();

			if(octetWriter.Position > Constants.MaxDatagramOctetSize)
			{
				throw new Exception($"too many predicted steps to serialize");
			}

			var datagram = new ClientDatagram
			{
				payload = octetWriter.Octets.ToArray()
			};
			clientOutDatagrams.Add(datagram);
		}

		private PredictedStepsForAllLocalPlayers FilterOutStepsToSend()
		{
			var predictedStepsForPlayers = new List<PredictedStepsForPlayer>();
			var localPlayerCount = predictedSteps.predictedStepsQueues.Count;
			if(localPlayerCount == 0)
			{
				return default;
			}

			var maxOctetSizePerPlayer = MaxOctetSize / localPlayerCount;
			foreach (var (playerIndex, predictedStepsQueue) in predictedSteps.predictedStepsQueues)
			{
				var allPredictedSteps = predictedStepsQueue.Collection;
				if(allPredictedSteps.Length == 0)
				{
					continue;
				}
				log.Debug("prepare predictedStep for {{PlayerIndex}}", playerIndex);

				var octetCount = 0;
				var stepCount = 0;
				foreach (var predictedStep in allPredictedSteps)
				{
					log.Debug($"prepare predictedStep: {{PlayerIndex}} {{TickID}}", playerIndex, predictedStep.appliedAtTickId);
					octetCount += predictedStep.payload.Length + 2;
					if(octetCount > maxOctetSizePerPlayer)
					{
						break;
					}

					stepCount++;
				}

				if(stepCount == 0)
				{
					log.Notice("didnt have room to add a single step into the buffer {{MaxOctetSizePerPlayer}}",
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