/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.BlobStream;
using Piot.Clog;
using Piot.Datagram;
using Piot.Discoid;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Nimble.AuthoritativeReceiveStatus;
using Piot.Nimble.Serialize;
using Piot.OrderedDatagrams;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public class HostConnection
	{
		public enum State
		{
			StartingUpload,
			UploadingSerializedSimulationState,
			Playing
		}

		public ConnectionToParticipants connectionToParticipants = new();

		public byte transportConnectionId;
		public MonotonicTimeLowerBits lastReceivedMonotonicLowerBits;
		public TickId expectingAuthoritativeTickId;
		public byte dropppedAuthoritativeAfterExpecting;
		public TickId lastReceivedPredictedTickId;
		public OrderedDatagramsSequenceId datagramSequenceIdOut;
		public BlobStreamSendLogic blobStreamSendLogic;
		public BlobStreamSender blobStreamSender;
		public State state;
		public ILog log;
		private AuthoritativeStepsQueue authoritativeStepsQueue;
		private Participants participants;
		private BlobStreamSenderChunks blobStreamSenderChunks;

		public HostConnection(byte transportConnectionId, BlobStreamSenderChunks serializedSavedState,
			AuthoritativeStepsQueue queue, Participants participants, TimeMs now,
			ILog log)
		{
			this.log = log;
			this.blobStreamSenderChunks = serializedSavedState;
			this.transportConnectionId = transportConnectionId;
			this.participants = participants;
			this.authoritativeStepsQueue = queue;
			blobStreamSender = new BlobStreamSender(serializedSavedState, now, log.SubLog("BlobStreamOut"));
			blobStreamSendLogic = new BlobStreamSendLogic(blobStreamSender, log.SubLog("BlobStreamOutLogic"));
		}

		public override string ToString()
		{
			return
				$"[HostConnection {transportConnectionId} participantCount:{connectionToParticipants.connections.Count}]";
		}

		public void WriteDatagramHeader(OctetWriter reusableOctetWriter)
		{
			OrderedDatagramsSequenceIdWriter.Write(reusableOctetWriter, datagramSequenceIdOut);
			datagramSequenceIdOut.Next();
			MonotonicTimeLowerBitsWriter.Write(lastReceivedMonotonicLowerBits, reusableOctetWriter);
		}

		public void HandleRequestAddPredictedSteps(Participants participants, OctetReader reader)
		{
			log.DebugLowLevel("handle predicted steps");
			StatusReader.Read(reader, out var expectingTickId, out var droppedTicksAfterThat);
			expectingAuthoritativeTickId = expectingTickId;
			dropppedAuthoritativeAfterExpecting = droppedTicksAfterThat;

			var highestTickId = PredictedStepsReader.Read(reader,
				transportConnectionId,
				connectionToParticipants, participants, log);
			if(highestTickId > lastReceivedPredictedTickId)
			{
				lastReceivedPredictedTickId = highestTickId;
			}
		}


		private void HandleAckDownloadStart(OctetReader reader)
		{
			var octetCount = reader.ReadUInt32();
			log.DebugLowLevel("handle ack download start {OctetCount}", octetCount);
			if(state == State.StartingUpload)
			{
				log.Debug("start uploading serialized save state to client {OctetCount}",
					blobStreamSenderChunks.OctetCount);
				state = State.UploadingSerializedSimulationState;
			}
		}

		public void HandleAckSerializedBlobStream(OctetReader reader)
		{
			log.DebugLowLevel("handle *ack serialized blob stream*");
			blobStreamSendLogic.ReceiveStream(reader);
			if(!blobStreamSendLogic.IsReceivedByRemote)
			{
				return;
			}

			if(state == State.UploadingSerializedSimulationState)
			{
				log.Debug("state is received by client, switching to state 'Playing'");
				state = State.Playing;
			}
		}

		public void WriteDatagrams(OctetWriter cachedWriter, CircularBuffer<HostDatagram> outDatagrams, TimeMs now)
		{
			switch (state)
			{
				case State.StartingUpload:
				{
					log.DebugLowLevel("sending starting upload to client {OctetCount}",
						blobStreamSenderChunks.OctetCount);
					cachedWriter.Reset();
					WriteDatagramHeader(cachedWriter);
					cachedWriter.WriteUInt8((byte)ClientCommand.StartDownload);
					cachedWriter.WriteUInt32(blobStreamSenderChunks.OctetCount);
					ref var outDatagram = ref outDatagrams.EnqueueRef();
					outDatagram.connection = transportConnectionId;
					outDatagram.payload = cachedWriter.Octets.ToArray();
					log.DebugLowLevel("complete StartingUpload size: {OctetCount}", outDatagram.payload.Length);
				}
					break;
				case State.UploadingSerializedSimulationState:
				{
					var chunksToSend = blobStreamSendLogic.PrepareChunksToSend(now);
					if(blobStreamSendLogic.IsReceivedByRemote)
					{
						state = State.Playing;
					}
					log.DebugLowLevel("uploading serialized state {Chunks}", chunksToSend);
					foreach (var chunkIndex in chunksToSend)
					{
						cachedWriter.Reset();
						WriteDatagramHeader(cachedWriter);
						cachedWriter.WriteUInt8((byte)ClientCommand.DownloadSerializedSaveState);
						blobStreamSendLogic.WriteStream(cachedWriter, chunkIndex);
						ref var outDatagram = ref outDatagrams.EnqueueRef();
						outDatagram.connection = transportConnectionId;
						outDatagram.payload = cachedWriter.Octets.ToArray();
					}
				}
					break;

				case State.Playing:
				{
					var authoritativeRangeInBuffer = authoritativeStepsQueue.Range;
					if(expectingAuthoritativeTickId < authoritativeRangeInBuffer.startTickId)
					{
						log.Notice(
							"{Connection} is way behind, it is waiting for {TickID} and authoritative buffer only has {Range}",
							this,
							expectingAuthoritativeTickId, authoritativeRangeInBuffer);
					}

					var startId = expectingAuthoritativeTickId;
					if(startId > authoritativeStepsQueue.Last.appliedAtTickId)
					{
						startId = authoritativeStepsQueue.Last.appliedAtTickId;
					}

					log.DebugLowLevel("sending authoritative steps {StartTickId} {RequestedTickId}", startId,
						expectingAuthoritativeTickId);

					cachedWriter.Reset();
					WriteDatagramHeader(cachedWriter);
					cachedWriter.WriteUInt8((byte)ClientCommand.AuthoritativeSteps);
					WriteParticipantInfo(cachedWriter);
					WriteBufferInfo(cachedWriter);
					AuthoritativeStepsWriter.Write(authoritativeStepsQueue, startId, cachedWriter, log);
					ref var outDatagram = ref outDatagrams.EnqueueRef();
					outDatagram.connection = transportConnectionId;
					outDatagram.payload = cachedWriter.Octets.ToArray();
				}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(state));
			}
		}

		private void WriteBufferInfo(OctetWriter writer)
		{
			var lastReceivedTickId = lastReceivedPredictedTickId;
			var diff = (int)lastReceivedTickId.tickId - (int)authoritativeStepsQueue.WaitingForTickId.tickId + 1;

			diff = diff switch
			{
				> 127 => 127,
				< -127 => -127,
				_ => diff
			};

			log.DebugLowLevel("writing {BufferInfo} {LastReceived} {WaitingAuthoritativeTickId}", diff,
				lastReceivedTickId, authoritativeStepsQueue.WaitingForTickId);
			writer.WriteInt8((sbyte)diff);
		}

		void WriteParticipantInfo(OctetWriter writer)
		{
			var connectionParticipants = connectionToParticipants.connections;
			writer.WriteUInt8((byte)connectionParticipants.Count);
			foreach (var (localPlayerIndex, participant) in connectionParticipants)
			{
				writer.WriteUInt8(localPlayerIndex.Value);
				writer.WriteUInt8(participant.participantId.id);
			}
		}

		public void HandleHeader(OctetReader reader)
		{
			var sequenceId = OrderedDatagramsSequenceIdReader.Read(reader);
			lastReceivedMonotonicLowerBits = MonotonicTimeLowerBitsReader.Read(reader);
			log.DebugLowLevel("handle reader {SequenceID}", sequenceId);
		}

		public void HandleDatagram(OctetReader reader)
		{
			HandleHeader(reader);
			while (!reader.IsEmpty)
			{
				var clientToHostRequest = (ClientToHostRequest)reader.ReadUInt8();
				switch (clientToHostRequest)
				{
					case ClientToHostRequest.RequestAddPredictedStep:
						HandleRequestAddPredictedSteps(participants, reader);
						break;

					case ClientToHostRequest.AckSerializedSaveStateStart:
						HandleAckDownloadStart(reader);
						break;

					case ClientToHostRequest.AckSerializedSaveStateBlobStream:
						HandleAckSerializedBlobStream(reader);
						break;
					default:
						log.Notice("received unknown client to host {Request}", clientToHostRequest);
						break;
				}
			}
		}
	}
}