/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Threading;
using NUnit.Framework;
using Piot.MonotonicTime;

[TestFixture]
public class MonotonicTests
{
	[Test]
	public void TestWithSleep()
	{
		var monotonicTime = new MonotonicTimeMs();
		var first = monotonicTime.TimeInMs;
		Thread.Sleep(100);
		var second = monotonicTime.TimeInMs;
		Console.WriteLine($"first: {first}");
		Console.WriteLine($"second: {second} {second.ms - first.ms}");
		Assert.That(second.ms - first.ms, Is.InRange(98, 110));
	}
}