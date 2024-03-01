using System.Collections.Generic;
using Piot.Nimble.Steps;
using Piot.Datagram;

namespace Piot.Nimble.Client
{
	public class NimbleSendClient
	{
		public PredictedStepsLocalPlayers predictedSteps;

		public NimbleSendClient()
		{
			predictedSteps = new PredictedStepsLocalPlayers();
		}

		public IEnumerable<ClientDatagram> OutDatagrams { get; set; }

		public void Tick()
		{
			throw new System.NotImplementedException();
		}
	}
}