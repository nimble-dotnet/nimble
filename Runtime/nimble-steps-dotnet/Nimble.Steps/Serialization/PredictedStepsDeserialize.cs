/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Piot.Clog;
using Piot.Flood;
using Piot.Tick.Serialization;

namespace Piot.Nimble.Steps.Serialization
{
	public static class PredictedStepsDeserialize
	{
		/// <summary>
		///     Deserializes game specific steps arriving on the host from the client.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static PredictedStepsForAllLocalPlayers Deserialize(IOctetReader reader, ILog log)
		{
			OctetMarker.AssertMarker(reader, Constants.PredictedStepsHeaderMarker);
			var localPlayerCount = reader.ReadUInt8();
			if(localPlayerCount == 0)
			{
				log.Notice("no predicted steps from client");
				return new(Array.Empty<PredictedStepsForPlayer>());
			}

			var players = new List<PredictedStepsForPlayer>();
			for (var localPlayerIndex = 0; localPlayerIndex < localPlayerCount; ++localPlayerIndex)
			{
				var stepCount = reader.ReadUInt8();
				var localPlayerId = reader.ReadUInt8();

				if(stepCount == 0)
				{
					throw new Exception($"zero count payload is not allowed");
				}

				var array = new PredictedStep[stepCount];

				var firstFrameId = TickIdReader.Read(reader);

				for (var i = 0; i < stepCount; ++i)
				{
					OctetMarker.AssertMarker(reader, Constants.PredictedStepsPayloadHeaderMarker);
					var payloadOctetCount = reader.ReadUInt8();
					if(payloadOctetCount > 20)
					{
						throw new($"suspicious step predicted step octet count {payloadOctetCount}");
					}

//					log.Debug("deserialize predicted payload {PayloadOctetCount}", payloadOctetCount);

					var predictedStep = new PredictedStep(new((uint)(firstFrameId.tickId + i)),
						reader.ReadOctets(payloadOctetCount));

					///					log.Debug("deserialized predicted step {{LocalPlayerId}} {{PredictedStep}}", localPlayerId,
					//					predictedStep);

					array[i] = predictedStep;
				}

				var play = new PredictedStepsForPlayer(new(localPlayerId), array);
				players.Add(play);
			}

			return new(players.ToArray());
		}
	}
}