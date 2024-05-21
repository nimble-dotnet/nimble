/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Piot.Replay.Serialization
{
    /// <summary>
    /// Representation of a complete and full simulation state.
    /// Must include all data to be completely deterministic
    /// </summary>
    public readonly struct SimulationStateEntry
    {
        public readonly ulong timeMs;
        public readonly uint tickId;
        public readonly ulong streamPosition;

        public SimulationStateEntry(ulong timeMs, uint tickId, ulong streamPosition)
        {
            this.timeMs = timeMs;
            this.tickId = tickId;
            this.streamPosition = streamPosition;
        }

        public override string ToString()
        {
            return $"[SimulationState tickID:{tickId} timeMs:{timeMs} streamPosition:{streamPosition}]";
        }
    }
}