using System;
using System.Collections.Generic;
using System.Linq;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Steps.Serialization;
using Piot.OrderedDatagrams;
using Piot.Stats;
using Piot.Tick;
using Constants = Piot.Datagram.Constants;

namespace Piot.Nimble.Client
{
	public sealed class NimbleSendClient
	{
		public const uint MaxOctetSize = 1024;
		public readonly PredictedStepsLocalPlayers predictedSteps;
		private readonly ILog log;
		private readonly OctetWriter octetWriter = new(1024);
		private const int MaximumClientOutDatagramCount = 4;
		private readonly CircularBuffer<ClientDatagram> clientOutDatagrams = new(MaximumClientOutDatagramCount);

		private readonly StatPerSecond datagramCountPerSecond;
		private readonly StatPerSecond datagramBitsPerSecond;
		private readonly StatPerSecond predictedStepsSentPerSecond;

		public FormattedStat DatagramCountPerSecond =>
			new(StandardFormatterPerSecond.Format, datagramCountPerSecond.Stat);

		public FormattedStat DatagramBitsPerSecond =>
			new(BitsPerSecondFormatter.Format, datagramBitsPerSecond.Stat);

		private OrderedDatagramsSequenceId datagramSequenceId;
		private TickId expectingAuthoritativeTickId;
		private TickId lastSentPredictedTickId;
		private TickId lastSentPredictedTickIdAddedToStats;
		
		public FormattedStat PredictedStepsSentPerSecond =>
			new(StandardFormatterPerSecond.Format, predictedStepsSentPerSecond.Stat);
		
		public NimbleSendClient(TimeMs now, ILog log)
		{
			this.log = log;
			datagramCountPerSecond = new StatPerSecond(now, new(500));
			datagramBitsPerSecond = new StatPerSecond(now, new(500));
			predictedStepsSentPerSecond = new StatPerSecond(now, new(500));
			predictedSteps = new PredictedStepsLocalPlayers();
		}

		public IEnumerable<ClientDatagram> OutDatagrams => clientOutDatagrams;


		public void Tick(TimeMs now)
		{
			var filteredOutPredictedStepsForLocalPlayers = FilterOutStepsToSend();

			octetWriter.Reset();

			OrderedDatagramsSequenceIdWriter.Write(octetWriter, datagramSequenceId);
			datagramSequenceId.Next();

			MonotonicTimeLowerBitsWriter.Write(
				new((ushort)(now.ms & 0xffff)), octetWriter);

			StatusWriter.Write(octetWriter, expectingAuthoritativeTickId, 0);

			PredictedStepsSerialize.Serialize(octetWriter, filteredOutPredictedStepsForLocalPlayers, log);


//			log.Warn($"decision to send predicted steps to send to the host {filteredOutPredictedStepsForLocalPlayers} {{OctetCount}}", octetWriter.Position);

			clientOutDatagrams.Clear();

			if(octetWriter.Position > Constants.MaxDatagramOctetSize)
			{
				throw new Exception($"too many predicted steps to serialize");
			}

			ref var datagram = ref clientOutDatagrams.EnqueueRef();
			datagram.payload = octetWriter.Octets.ToArray();

			var diff = lastSentPredictedTickId.tickId - lastSentPredictedTickIdAddedToStats.tickId;
			if (diff > 0)
			{
				predictedStepsSentPerSecond.Add((int)diff);
			}
			lastSentPredictedTickIdAddedToStats = lastSentPredictedTickId;

			datagramCountPerSecond.Add(1);
			datagramBitsPerSecond.Add(datagram.payload.Length * 8);

			datagramCountPerSecond.Update(now);
			datagramBitsPerSecond.Update(now);
			predictedStepsSentPerSecond.Update(now);
		}

		public void OnLatestAuthoritativeTickId(TickId tickId, uint droppedCount)
		{
			predictedSteps.DiscardUpToAndExcluding(tickId.Next);
			expectingAuthoritativeTickId = tickId.Next;
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
				if(predictedStepsQueue.IsEmpty)
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
					if(octetCount > maxOctetSizePerPlayer)
					{
						log.Debug("we reached our limit, break here {OctetCount} {MaxOctetSizePerPlayer}", octetCount,
							maxOctetSizePerPlayer);
						break;
					}

					stepCount++;
				}

				if(stepCount == 0)
				{
					log.Notice("didnt have room to add a single step into the buffer {MaxOctetSizePerPlayer}",
						maxOctetSizePerPlayer);
				}


				var filteredOutSteps = allPredictedSteps.Take(stepCount);
				var array = filteredOutSteps.ToArray();

				if (array.Length > 0)
				{
					lastSentPredictedTickId = array[^1].appliedAtTickId;
				}
				var predictedStepsForOnePlayer =
					new PredictedStepsForPlayer(new(playerIndex), array);
				predictedStepsForPlayers.Add(predictedStepsForOnePlayer);
			}

			var allPlayers = new PredictedStepsForAllLocalPlayers(predictedStepsForPlayers.ToArray());

			return allPlayers;
		}
	}
}