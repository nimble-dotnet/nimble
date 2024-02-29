using Piot.Clog;
using Piot.Tick;

namespace Nimble.Authoritative.Steps
{
	public class CombinedAuthoritativeStepProducer
	{
		private ParticipantConnections connections;
		private CombinedAuthoritativeStepsQueue combinedAuthoritativeStepsQueue;
		private TickId tickId;
		private ILog log;

		public CombinedAuthoritativeStepsQueue AuthoritativeStepsQueue => combinedAuthoritativeStepsQueue;

		public CombinedAuthoritativeStepProducer(TickId tickId, ParticipantConnections connections, ILog log)
		{
			this.tickId = tickId;
			this.log = log;
			this.connections = connections;
		}

		public CombinedAuthoritativeStep ComposeOneStep()
		{
			var combinedAuthoritativeStep = Combiner.ComposeOneAuthoritativeSteps(connections, tickId, log);

			combinedAuthoritativeStepsQueue.Add(combinedAuthoritativeStep);

			return combinedAuthoritativeStep;
		}
	}
}