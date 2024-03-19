/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Text;
using Piot.Flood;

namespace Piot.Guise
{
    public static class GuiseReader
    {
        public static void ReadChallenge(IOctetReader reader, out ClientNonce clientNonce,
            out ServerChallenge serverChallenge)
        {
            clientNonce = ReadClientNonce(reader);
            serverChallenge = ReadServerChallenge(reader);
        }

        private static ServerChallenge ReadServerChallenge(IOctetReader reader)
        {
            return new ServerChallenge
            {
                value = reader.ReadUInt64()
            };
        }

        private static ClientNonce ReadClientNonce(IOctetReader reader)
        {
            return new ClientNonce()
            {
                value = reader.ReadUInt64()
            };
        }

        public static string ReadString(IOctetReader reader)
        {
            var length = reader.ReadUInt8();
            return Encoding.UTF8.GetString(reader.ReadOctets(length));
        }
        
        public static UserName ReadUserName(IOctetReader reader)
        {
            OctetMarker.AssertMarker(reader, 0x94);
            return new UserName()
            {
                value = ReadString(reader)
            };
        }
        
        private static LoggedIn ReadLogin(IOctetReader reader)
        {
            return new LoggedIn()
            {
                clientNonce = ReadClientNonce(reader),
                userName = ReadUserName(reader),
                userSessionId = ReadUserSessionId(reader),
            };
        }

        private static UserSessionId ReadUserSessionId(IOctetReader reader)
        {
            return new UserSessionId
            {
                value = reader.ReadUInt64()
            };
        }
    }
}