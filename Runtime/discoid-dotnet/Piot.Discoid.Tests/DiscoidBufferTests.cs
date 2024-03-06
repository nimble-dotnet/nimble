/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/discoid-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using NUnit.Framework;
using Piot.Discoid;

[TestFixture]
public class DiscoidBufferTests
{
	[Test]
	public void SetBitsAndEnumerator()
	{
		var queue = new DiscoidBuffer<int>(4);
		Assert.AreEqual(0, queue.Bits());

		queue[1] = 99;
		queue[3] = 42;

		Assert.AreEqual(0b1010, queue.Bits());
		queue.DiscardFront(1);
		Assert.AreEqual(0b0101, queue.Bits());

		Assert.IsTrue(queue.TryGet(0, out var findAt0));
		Assert.AreEqual(99, findAt0);
		Assert.IsFalse(queue.TryGet(1, out _));
		Assert.IsTrue(queue.TryGet(2, out var findAt2));
		Assert.AreEqual(42, findAt2);

		using var enumerable = queue.GetEnumerator();
		Assert.IsTrue(enumerable.MoveNext());
		Assert.AreEqual(99, enumerable.Current);
		Assert.IsFalse(enumerable.MoveNext());

		foreach (var x in queue)
		{
			Console.WriteLine($"found: {x}");
		}
	}
}