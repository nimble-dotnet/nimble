using System.Collections.Generic;
using Piot.Nimble.Steps;
using Piot.Datagram;

namespace Piot.Nimble.Client
{
	public class NimbleSendClient
	{
		public PredictedStepsLocalPlayers predictedSteps;
		private ClientDatagram fakeDatagram;

		public NimbleSendClient()
		{
			predictedSteps = new PredictedStepsLocalPlayers();
			fakeDatagram = new ClientDatagram
			{
				payload = new byte[] { 0xc0, 0xde }
			};
		}

		public IEnumerable<ClientDatagram> OutDatagrams => new List<ClientDatagram> { fakeDatagram };

		public void Tick()
		{
		}
	}
}