/*---------------------------------------------------------------------------------------------
 * Copyright (c) Peter Bjorklund. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using Piot.Flood;
using Piot.MonotonicTime;
using Piot.MonotonicTimeLowerBits;
using Piot.Raff.Stream;
using Piot.SerializableVersion.Serialization;
using Piot.Tick;
using Piot.Tick.Serialization;

namespace Piot.Replay.Serialization
{
	/// <summary>
	/// Represents the configuration for replay writer and reader.
	/// </summary>
	public struct ReplayWriterConfig
	{
		public uint maximumStepOrStateOctetSize;
		public uint framesUntilCompleteState;
	}
}