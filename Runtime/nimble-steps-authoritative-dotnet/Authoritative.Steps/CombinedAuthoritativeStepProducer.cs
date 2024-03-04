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
		public uint timer;

		public CombinedAuthoritativeStepsQueue AuthoritativeStepsQueue => combinedAuthoritativeStepsQueue;

		public CombinedAuthoritativeStepProducer(TickId tickId, ParticipantConnections connections, ILog log)
		{
			this.tickId = tickId;
			this.log = log;
			this.connections = connections;
			combinedAuthoritativeStepsQueue = new CombinedAuthoritativeStepsQueue(tickId);
		}

		private CombinedAuthoritativeStep ComposeOneStep()
		{
			var combinedAuthoritativeStep = Combiner.ComposeOneAuthoritativeSteps(connections, tickId, log);
			tickId = tickId.Next;

			combinedAuthoritativeStepsQueue.Add(combinedAuthoritativeStep);

			return combinedAuthoritativeStep;
		}

		private bool TryToComposeOneStep()
		{
			var percentageThatAreReady = connections.PercentageThatHasPredictedStepForAtLeast(tickId);
			if(percentageThatAreReady == 100 || timer > 3)
			{
				if(percentageThatAreReady == 100)
				{
					log.Debug("{{Percentage}} says that we should compose am authoritative step for {{TickID}}",
						percentageThatAreReady, tickId);
				}
				else
				{
					log.Debug("{{Timeout}} says that we should compose an authoritative step for {{TickID}}", timer, tickId);
				}

				ComposeOneStep();
				timer = 0;
				return true;
			}
			
			if(percentageThatAreReady >= 50)
			{
				timer++;
			}
			else
			{
				log.Debug("only {{Percentage}} are ready for {{TickID}} , waiting with authoritative step", percentageThatAreReady, tickId);
			}

			return false;
		}

		public void Tick()
		{
			while (TryToComposeOneStep())
			{
				
			}
		}
	}
}