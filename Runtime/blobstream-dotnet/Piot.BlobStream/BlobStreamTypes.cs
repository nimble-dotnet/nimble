/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
namespace Piot.BlobStream
{
    public enum ClientReceiveCommand : byte
    {
        SetChunk = 85
    }
    
    public enum ClientSendCommand : byte
    {
        AckChunk = 53
    }
}