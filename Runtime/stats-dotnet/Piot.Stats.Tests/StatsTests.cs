/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/nimble-steps-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System.Diagnostics;
using NUnit.Framework;
using Piot.MonotonicTime;
using Piot.Stats;

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
public class StatsTests
{
	[Test]
	public void VerifyStatsCalculation()
	{
//        var outputLogger = new ConsoleOutputWithoutColorLogger();
		//      var log = new Log(outputLogger);

		var now = new TimeMs(1000);
		var minimumAverageTime = new FixedDeltaTimeMs(1);

		var x = new StatPerSecond(now, minimumAverageTime);
		x.Add(10);
		var after = now + new FixedDeltaTimeMs(500);
		x.Update(after);
		Assert.AreEqual(20, x.Stat.average);
	}
}