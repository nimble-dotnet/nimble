/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Piot.Raff;
using Piot.SerializableVersion;

namespace Piot.Replay.Serialization
{
    public static class Constants
    {
        public static FourCC ReplayName = FourCC.Make("rps1");
        public static FourCC ReplayIcon = new(0xF09F8EAC); // Clapper board

        public static FourCC CompleteStateName = FourCC.Make("rst1");
        public static FourCC CompleteStateIcon = new(0xF09F93B8); // Camera with flash

        public static FourCC AuthoritativeStepName = FourCC.Make("ras1");
        public static FourCC AuthoritativeStepIcon = new(0xF09F8EAE); // Gamepad

        public static SemanticVersion ReplayFileVersion = new(0x0, 0x0, 0x1);
    }
}