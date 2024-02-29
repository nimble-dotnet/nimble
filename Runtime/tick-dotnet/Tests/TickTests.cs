/*----------------------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved. https://github.com/piot/tick-dotnet
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------------------*/

using NUnit.Framework;
using Piot.Flood;
using Piot.Tick;
using Piot.Tick.Serialization;

[TestFixture]
public class TickTests
{
	[Test]
	public void TickIdSerialization()
	{
		var writer = new OctetWriter(1024);
		TickIdWriter.Write(writer, new TickId(2049));
		var readTickId = TickIdReader.Read(new OctetReader(writer.Octets));

		Assert.That(readTickId.tickId, Is.EqualTo(2049));
	}
}