/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using Piot.Clog;
using Piot.Flood;
using Piot.Nimble.Steps;
using Piot.Nimble.Steps.Serialization;
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
public class PredictedStepsTests
{
	[Test]
	public void SerializationOfLocalParticipants()
	{
		const byte ExpectedLocalPlayerIndexValue = 7;
		const uint ExpectedTickIdValue = 42;
		byte[] ExpectedStepPayload = { 0x24 };

		var writer = new OctetWriter(1024);
		var oneStep = new PredictedStep(new TickId(ExpectedTickIdValue), ExpectedStepPayload);
		var queue = new PredictedStepsQueue(new(0));
		queue.AddPredictedStep(oneStep);
		var outputLogger = new ConsoleOutputWithoutColorLogger();
		var log = new Log(outputLogger);

		var playerId = new LocalPlayerIndex(ExpectedLocalPlayerIndexValue);
		var forOnePlayer = new PredictedStepsForPlayer(playerId, queue.Collection.ToArray());
		var allLocalPlayers = new PredictedStepsForAllLocalPlayers(new[] { forOnePlayer });
		PredictedStepsSerialize.Serialize(writer, allLocalPlayers, log);

		var reader = new OctetReader(writer.Octets);
		var deserializedAllPlayers = PredictedStepsDeserialize.Deserialize(reader, log);
		var deserializedFirstPlayer = deserializedAllPlayers.stepsForEachPlayerInSequence.First();
		Assert.That(deserializedFirstPlayer.localPlayerIndex.Value, Is.EqualTo(ExpectedLocalPlayerIndexValue));
		Assert.That(deserializedFirstPlayer.steps.First().appliedAtTickId.tickId, Is.EqualTo(ExpectedTickIdValue));
		Assert.That(deserializedFirstPlayer.steps.First().payload.ToArray(), Is.EqualTo(ExpectedStepPayload));
	}
}