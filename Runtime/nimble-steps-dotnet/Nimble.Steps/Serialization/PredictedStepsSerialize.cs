/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Runtime.CompilerServices;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick.Serialization;

namespace Piot.Nimble.Steps.Serialization
{
	public static class Constants
	{
		public const byte PredictedStepsHeaderMarker = 0xdb;
		public const byte PredictedStepsPayloadHeaderMarker = 0xdc;
	}

	public static class PredictedStepsSerialize
	{
		/// <summary>
		///     Serializing the game specific inputs to be sent from the client to the authoritative host.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Serialize(IOctetWriter writer, PredictedStepsForAllLocalPlayers inputsForLocalPlayers,
			ILog log)
		{
			OctetMarker.WriteMarker(writer, Constants.PredictedStepsHeaderMarker);

			if(inputsForLocalPlayers.stepsForEachPlayerInSequence.Length == 0)
			{
				log.Notice("no input to serialize!");
			}

			writer.WriteUInt8((byte)inputsForLocalPlayers.stepsForEachPlayerInSequence.Length);

			foreach (var stepsForPlayer in inputsForLocalPlayers.stepsForEachPlayerInSequence)
			{
				var tickCount = stepsForPlayer.steps.Length;
				if(tickCount > 255)
				{
					throw new("too many inputs to serialize");
				}

				if(tickCount == 0)
				{
					log.Notice("no input (tickCount is zero) to serialize!");
				}


				writer.WriteUInt8((byte)tickCount);
				if(tickCount == 0)
				{
					continue;
				}

				writer.WriteUInt8(stepsForPlayer.localPlayerIndex.Value);

				var first = stepsForPlayer.steps[0];
				TickIdWriter.Write(writer, first.appliedAtTickId);
//				log.Notice("first {TickCount} {LocalPlayerID}  {TickID}", tickCount,
//					stepsForPlayer.localPlayerIndex, first.appliedAtTickId);
				var expectedTickIdValue = first.appliedAtTickId.tickId;

				foreach (var predictedStep in stepsForPlayer.steps)
				{
					if(predictedStep.appliedAtTickId.tickId != expectedTickIdValue)
					{
						throw new(
							$"predicted step in wrong order in collection. Expected {expectedTickIdValue} but received {predictedStep.appliedAtTickId.tickId}");
					}

					OctetMarker.WriteMarker(writer, Constants.PredictedStepsPayloadHeaderMarker);

//					log.Debug("writing predicted step {{TickID}} {{PayloadLength}}", predictedStep.appliedAtTickId,
//						(byte)predictedStep.payload.Length);
					writer.WriteUInt8((byte)predictedStep.payload.Length);
					writer.WriteOctets(predictedStep.payload.Span);

					expectedTickIdValue++;
				}
			}
		}
	}
}