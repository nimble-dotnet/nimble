using System;
using System.Collections;
using System.Collections.Generic;
using Nimble.Authoritative.Steps;
using Piot.Clog;
using Piot.Datagram;
using Piot.Nimble.Steps;
using Piot.Tick;

namespace Piot.Nimble.Host
{
	public class NimbleHost
	{
		public CombinedAuthoritativeStepProducer authoritativeStepProducer;
		public ParticipantConnections participantConnections = new();

		private HostDatagram fakeDatagram;

		public NimbleHost(TickId startId, ILog log)
		{
			authoritativeStepProducer = new CombinedAuthoritativeStepProducer(startId, participantConnections, log);
			fakeDatagram = new HostDatagram
			{
				connection = 0,
				payload = new byte[] { 0xca, 0xfe }
			};
		}

		public void ReceiveDatagram(in HostDatagram datagram)
		{
			var participantConnection = participantConnections.GetParticipantConnection(new ParticipantId(0));
			participantConnection.incomingSteps.AddPredictedStep(new PredictedStep());
		}

		public void Tick(TickId simulationTickId)
		{
			authoritativeStepProducer.ComposeOneStep();
		}

		public IEnumerable<HostDatagram> DatagramsToSend => new List<HostDatagram> { fakeDatagram };
	}
}