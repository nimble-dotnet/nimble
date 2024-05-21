/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Piot.Flood;
using Piot.Raff;
using Piot.Raff.Stream;
using Piot.Tick.Serialization;

namespace Piot.Replay.Serialization
{
    public static class SimulationStateScanner
    {
        public static SimulationStateEntry[] ScanForSimulationStatesInStream(RaffReader tempRaffReader,
            IOctetReaderWithSeekAndSkip readerWithSeek, bool allowBrokenStream = false)
        {
            var entries = new List<SimulationStateEntry>();

            while (true)
            {
                try
                {
                    var positionForLastHeader = readerWithSeek.Position;
                    var octetLength = tempRaffReader.ReadChunkHeader(out var icon, out _);
                    if (octetLength == 0)
                    {
                        break;
                    }

                    var positionAfterChunkHeaderBefore = readerWithSeek.Position;
                    if (icon.Value == Constants.CompleteStateIcon.Value)
                    {
                        var packType = readerWithSeek.ReadUInt8();
                        if (packType != 0x02)
                        {
                            if (allowBrokenStream)
                            {
                                break;
                            }
                            
                            throw new SerializationException("Corrupt replay file");
                        }

                        var time = readerWithSeek.ReadUInt64();
                        var tickId = TickIdReader.Read(readerWithSeek);
                        entries.Add(new SimulationStateEntry(time, tickId.tickId, positionForLastHeader));
                    }

                    readerWithSeek.Seek(positionAfterChunkHeaderBefore + octetLength);
                }
                catch (EndOfStreamException)
                {
                    if (!allowBrokenStream)
                    {
                        throw;
                    }

                    break;
                }
            }

            return entries.ToArray();
        }
    }
}