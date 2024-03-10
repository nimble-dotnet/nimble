/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.Replay.Serialization;
using Piot.SerializableVersion;


namespace Piot.TransportReplay
{
	public sealed class ClientTransportPlayback
	{
		readonly long adjustTimeMs;
		readonly ReplayReader replayPlayback;
		readonly IMonotonicTimeMs timeProvider;
		bool isEndOfStream;
		DeltaState nextDeltaState;

		public ClientTransportPlayback(IOctetSerializableRead state, SemanticVersion applicationSemanticVersion,
			IOctetReaderWithSeekAndSkip readerWithSeekAndSkip, IMonotonicTimeMs timeProvider)
		{
			this.timeProvider = timeProvider;
			replayPlayback = new(applicationSemanticVersion, Constants.ReplayInfo, readerWithSeekAndSkip);
			var completeState = replayPlayback.Seek(new(0));
			InitialTimeMs = completeState.CapturedAtTimeMs;

			var reader = new OctetReader(completeState.Payload);
			state.Deserialize(reader);

			var deltaState = replayPlayback.ReadDeltaState();

			nextDeltaState = deltaState ?? throw new("too short");
		}

		public TimeMs InitialTimeMs { get; }

		public ReadOnlySpan<byte> Read()
		{
			if(timeProvider.TimeInMs.ms < nextDeltaState.TimeProcessedMs.ms || isEndOfStream)
			{
				return ReadOnlySpan<byte>.Empty;
			}

			var reader = new OctetReader(nextDeltaState.Payload);

			var octetLength = reader.ReadUInt16();

			var octetsToReturn = reader.ReadOctets(octetLength);
			var next = replayPlayback.ReadDeltaState();
			if(next is null)
			{
				isEndOfStream = true;
			}
			else
			{
				nextDeltaState = next;
			}

			return octetsToReturn;
		}
	}
}