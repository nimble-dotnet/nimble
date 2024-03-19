/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using Piot.Flood;

namespace Piot.Guise
{
    public static class GuiseWriter
    {
        public static void Write(IOctetWriter writer, ClientNonce clientNonce, UserId userId,
            PasswordHashWithChallenge passwordHashWithChallenge)
        {
            WriteCommand(writer, SerializeCommand.Login);
            WriteClientNonce(writer, clientNonce);
            WriteUserId(writer, userId);
            WritePasswordHashWithChallenge(writer, passwordHashWithChallenge);
        }

        public static void WritePasswordHashWithChallenge(IOctetWriter writer,
            PasswordHashWithChallenge passwordHashWithChallenge)
        {
            OctetMarker.WriteMarker(writer, 0x99);
            writer.WriteUInt64(passwordHashWithChallenge.value);
        }

        public static void WriteUserId(IOctetWriter writer, UserId userId)
        {
            OctetMarker.WriteMarker(writer, 0x89);
            writer.WriteUInt64(userId.value);
        }

        public static void WriteClientNonce(IOctetWriter writer, ClientNonce clientNonce)
        {
            writer.WriteUInt64(clientNonce.value);
        }

        public static void WriteCommand(IOctetWriter writer, SerializeCommand cmd)
        {
            writer.WriteUInt8((byte)cmd);
        }
    }
}