/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using Nimble.Authoritative.Steps;
using NUnit.Framework;
using Piot.Clog;
using Piot.Flood;
using Piot.Nimble.Steps;
using Piot.Tick;

[SetUpFixture]
public class SetupTrace
{
	[OneTimeSetUp]
	public void StartTest()
	{
		Trace.Listeners.Add(new ConsoleTraceListener());
	}

	[OneTimeTearDown]
	public void EndTest()
	{
		Trace.Flush();
	}
}

[TestFixture]
public class AuthoritativeStepsTests
{
	public static PredictedStep CreateStep(uint expectedTickIdValue, byte someValue)
	{
		byte[] expectedStepPayload = { someValue };

		var oneStep = new PredictedStep(new TickId(expectedTickIdValue), expectedStepPayload);

		return oneStep;
	}

	public static void FillPredictedSteps(uint tickValue, byte count, PredictedStepsQueue target)
	{
		for (var i = 0; i < count; ++i)
		{
			var step = CreateStep((uint)(tickValue + i), (byte)(i + 16));
			target.AddPredictedStep(step);
		}
	}


	[Test]
	public void VerifyCombinedAuthoritativeStep()
	{
		var outputLogger = new ConsoleOutputWithoutColorLogger();
		var log = new Log(outputLogger);

		var startId = new TickId(13);

		var participantConnections = new ParticipantConnections();

		const int NUMBER_OF_PARTICIPANTS = 3;
		for (var playerIndex = 0; playerIndex < NUMBER_OF_PARTICIPANTS; ++playerIndex)
		{
			var participantConnection =
				participantConnections.CreateParticipantConnection(new ConnectionId(0));
			FillPredictedSteps((uint)(startId.tickId + playerIndex), 10, participantConnection.incomingSteps);
		}


		{
			var tooEarlyTickId = new TickId(2);
			var tooEarlyCombinedAuthoritativeStep =
				Combiner.ComposeOneAuthoritativeSteps(participantConnections, tooEarlyTickId, log);

			var writer = new OctetWriter(512);
			CombinedWriter.Write(tooEarlyCombinedAuthoritativeStep, writer);

			var reader = new OctetReader(writer.Octets);


			var combinedAuthoritativeStep =
				CombinedReader.DeserializeCombinedAuthoritativeStep(tooEarlyTickId, reader, log);

			Assert.That(combinedAuthoritativeStep.appliedAtTickId, Is.EqualTo(tooEarlyTickId));
			Assert.That(combinedAuthoritativeStep.authoritativeSteps.Count, Is.EqualTo(NUMBER_OF_PARTICIPANTS));
			var middleParticipant = new ParticipantId(1);
			var middleParticipantStep = combinedAuthoritativeStep.authoritativeSteps[middleParticipant];

			Assert.That(middleParticipantStep.appliedAtTickId, Is.EqualTo(tooEarlyTickId));
			Assert.That(middleParticipantStep.payload.Length, Is.Zero);
			Assert.That(middleParticipantStep.connectState,
				Is.EqualTo(SerializeProviderConnectState.StepNotProvidedInTime));
		}

		{
			var secondTickId = new TickId(14);
			var secondCombinedAuthoritativeStep =
				Combiner.ComposeOneAuthoritativeSteps(participantConnections, secondTickId, log);

			var writer = new OctetWriter(512);
			CombinedWriter.Write(secondCombinedAuthoritativeStep, writer);

			var reader = new OctetReader(writer.Octets);

			var combinedAuthoritativeStep =
				CombinedReader.DeserializeCombinedAuthoritativeStep(secondTickId, reader, log);

			Assert.That(combinedAuthoritativeStep.appliedAtTickId, Is.EqualTo(secondTickId));
			Assert.That(combinedAuthoritativeStep.authoritativeSteps.Count, Is.EqualTo(NUMBER_OF_PARTICIPANTS));
			var middleParticipant = new ParticipantId(1);
			var middleParticipantStep = combinedAuthoritativeStep.GetAuthoritativeStep(middleParticipant);

			Assert.That(middleParticipantStep.appliedAtTickId, Is.EqualTo(secondTickId));
			Assert.That(middleParticipantStep.payload.Length, Is.EqualTo(1));
			Assert.That(middleParticipantStep.connectState,
				Is.EqualTo(SerializeProviderConnectState.Normal));
			Assert.That(middleParticipantStep.payload.Span[0],
				Is.EqualTo(17 - middleParticipant.id));

			var laterParticipant = new ParticipantId(NUMBER_OF_PARTICIPANTS - 1);
			var laterParticipantStep = combinedAuthoritativeStep.GetAuthoritativeStep(laterParticipant);

			Assert.That(laterParticipantStep.appliedAtTickId, Is.EqualTo(secondTickId));
			Assert.That(laterParticipantStep.payload.Length, Is.EqualTo(0));
			Assert.That(laterParticipantStep.connectState,
				Is.EqualTo(SerializeProviderConnectState.StepNotProvidedInTime));
		}
	}
}