// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ST.VideoIntegration.Server;

namespace Services
{
    internal class VideoIntegrationService : IVideoIntegration
    {
        public string GetOnlineVideoUrl(int monObjId, int camNum, int streamType)
            => @$"https://cpweb.st-hld.ru/parrams?monObjId={monObjId}&camNum={camNum}&streamType={streamType}";
    }
}
