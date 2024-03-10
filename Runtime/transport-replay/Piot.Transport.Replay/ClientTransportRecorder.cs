/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.Replay.Serialization;
using Piot.Tick;
using Piot.TransportReplay;

namespace Piot.TransportReplay
{
	public class ClientTransportRecorder
	{
		readonly OctetWriter cachedBuffer = new(Piot.Datagram.Constants.MaxDatagramOctetSize + 4);
		readonly IMonotonicTimeMs timeProvider;
		readonly ReplayWriter writer;

		public ClientTransportRecorder(IOctetSerializableWrite state,
			ReplayVersionInfo replayVersionInfo,
			IMonotonicTimeMs timeProvider, TickId tickId, IOctetWriter target)
		{
			TickId = tickId;
			this.timeProvider = timeProvider;
			var stateWriter = new OctetWriter(32 * 1024);
			state.Serialize(stateWriter);
			var complete = new CompleteState(timeProvider.TimeInMs, tickId, stateWriter.Octets);
			const int framesBetweenCompleteState = 0;
			writer = new(complete, replayVersionInfo, Constants.ReplayInfo,
				target, framesBetweenCompleteState);
		}

		public TickId TickId { get; set; }

		public void Write(ReadOnlySpan<byte> datagram)
		{
			cachedBuffer.Reset();
			cachedBuffer.WriteUInt16((ushort)datagram.Length);
			cachedBuffer.WriteOctets(datagram);

			var delta = new DeltaState(timeProvider.TimeInMs, TickIdRange.FromTickId(TickId), cachedBuffer.Octets);
			writer.AddDeltaState(delta);
		}

		public void Close()
		{
			writer.Close();
		}
	}
}