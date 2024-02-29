using System;
using System.Collections;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Piot.Nimble.Host
{
	public struct NimbleHostDatagram
	{
		public ConnectionId connectionId;
		public Memory<byte> payload;
	}
	
	public class NimbleHost
	{
		public CombinedAuthoritativeStepProducer authoritativeStepProducer;
		public ParticipantConnections participantConnections = new();

		public NimbleHost(TickId startId, ILog log)
		{
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participantConnections, log);
		}

		public void ReceiveDatagram(ConnectionId connectionId, ReadOnlySpan<byte> span)
		{
			var participantConnection = participantConnections.GetParticipantConnection(new ParticipantId(0));
			participantConnection.incomingSteps.AddPredictedStep(new PredictedStep());
		}

		public void Update()
		{
			authoritativeStepProducer.ComposeOneStep();
		}

		public IEnumerable<NimbleHostDatagram> DatagramsToSend()
		{
			return new List<NimbleHostDatagram>();
		}
	}
}