/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Peter Bjorklund. All rights reserved.
 *  Licensed under the MIT License. See LICENSE in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using Piot.SerializableVersion;

namespace Piot.Replay.Serialization
{
    public readonly struct ReplayVersionInfo
    {
        public readonly SemanticVersion applicationSemanticVersion;
        public readonly SemanticVersion protocolSemanticVersion;

        public ReplayVersionInfo(SemanticVersion applicationSemanticVersion,
            SemanticVersion protocolSemanticVersion)
        {
            this.applicationSemanticVersion = applicationSemanticVersion;
            this.protocolSemanticVersion = protocolSemanticVersion;
        }
    }
}