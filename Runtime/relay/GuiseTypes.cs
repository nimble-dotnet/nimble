/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace Piot.Guise
{
    public enum SerializeCommand : byte
    {
        Login,
        Challenge
    }

    public struct ClientNonce
    {
        public ulong value;
    }

    public struct ServerChallenge
    {
        public ulong value;
    }

    public struct UserId
    {
        public ulong value;
    }

    public struct PasswordHashWithChallenge
    {
        public ulong value;
    }

    public struct LoggedIn
    {
        public ClientNonce clientNonce;
        public UserName userName;
        public UserSessionId userSessionId;
    }
    
    public struct UserName
    {
        public string value;
    }

    public struct UserSessionId
    {
        public ulong value;
    }
}