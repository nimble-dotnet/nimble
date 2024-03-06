/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/discoid-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using Piot.Discoid;

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
public class CircularBufferTests
{
	[Test]
	public void SimpleDequeueWithCount()
	{
		var queue = new CircularBuffer<int>(3);

		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);
		int dequeuedItem = queue.Dequeue();

		Assert.AreEqual(1, dequeuedItem);
		Assert.AreEqual(2, queue.Count);
	}

	[Test]
	public void EnqueueTooMuch()
	{
		var queue = new CircularBuffer<int>(3);

		queue.Enqueue(1);
		queue.Enqueue(2);
		queue.Enqueue(3);
		Assert.Throws<InvalidOperationException>(() => queue.Enqueue(99));
	}

	[Test]
	public void Peek()
	{
		var queue = new CircularBuffer<int>(10);

		queue.Enqueue(-42);
		queue.Enqueue(0);
		queue.Enqueue(99);

		var value = queue.Peek();
		Assert.AreEqual(-42, value);
	}

	[Test]
	public void PeekAhead()
	{
		var queue = new CircularBuffer<int>(10);

		queue.Enqueue(-42);
		queue.Enqueue(0);
		queue.Enqueue(99);

		var value = queue.Peek(2);
		Assert.AreEqual(99, value);
	}

	[Test]
	public void EnqueueMany()
	{
		var queue = new CircularBuffer<int>(3);
		Assert.IsFalse(queue.IsFull);
		queue.Enqueue(-42);
		queue.Enqueue(0);
		queue.Enqueue(99);

		Assert.IsTrue(queue.IsFull);
		Assert.AreEqual(3, queue.Count);
		for (var i = 0; i < 30; ++i)
		{
			queue.Dequeue();
			queue.Enqueue(i);
			Assert.AreEqual(3, queue.Count);
		}

		var value = queue.Peek();
		Assert.AreEqual(30 - 3, value);

		Assert.AreEqual(28, queue[1]);

		queue.Discard(2);
		Assert.AreEqual(1, queue.Count);

		Assert.AreEqual(29, queue.Dequeue());
	}

	[Test]
	public void EnumerateManually()
	{
		var queue = new CircularBuffer<int>(3);

		using var enumerator = queue.GetEnumerator();
		queue.Enqueue(-42);
		queue.Enqueue(0);
		Assert.IsFalse(queue.IsFull);
		queue.Enqueue(99);
		Assert.IsTrue(queue.IsFull);

		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(-42, enumerator.Current);

		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(0, enumerator.Current);

		Assert.IsTrue(enumerator.MoveNext());
		Assert.AreEqual(99, enumerator.Current);
		Assert.IsFalse(enumerator.MoveNext());
		
		Assert.AreEqual(3, queue.Count);
		queue.Clear();
		Assert.AreEqual(0, queue.Count);
		Assert.IsTrue(queue.IsEmpty);
		
	}


	[Test]
	public void EnumerateWithForEach()
	{
		var queue = new CircularBuffer<int>(3);

		using var enumerator = queue.GetEnumerator();
		queue.Enqueue(-42);
		queue.Enqueue(0);
		queue.Enqueue(99);

		var temp = new List<int>();
		foreach (var item in queue)
		{
			Console.WriteLine($"found {item}");
			temp.Add(item);
		}

		Assert.AreEqual(-42, temp[0]);
		Assert.AreEqual(0, temp[1]);
		Assert.AreEqual(99, temp[2]);

		for (var i = 0; i < 30; ++i)
		{
			queue.Dequeue();
			queue.Enqueue(i);
			Assert.AreEqual(3, queue.Count);
		}

		temp.Clear();
		foreach (var item in queue)
		{
			Console.WriteLine($"found {item}");
			temp.Add(item);
		}

		Assert.AreEqual(27, temp[0]);
		Assert.AreEqual(28, temp[1]);
		Assert.AreEqual(29, temp[2]);
	}
}