﻿//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#if USE_WEBRTC_API
using RtcCodecInfo = webrtc_winrt_api.CodecInfo;
#elif USE_ORTC_API
using RtcCodecInfo = ChatterBox.Client.Voip.Rtc.CodecInfo;
#endif //USE_WEBRTC_API

namespace ChatterBox.Client.Voip.Utils
{
    internal class SdpUtils
    {
        /// <summary>
        /// Forces the SDP to use the selected audio and video codecs.
        /// </summary>
        /// <param name="sdp">Session description.</param>
        /// <param name="audioCodec">Audio codec.</param>
        /// <param name="videoCodec">Video codec.</param>
        /// <returns>True if succeeds to force to use the selected audio/video codecs.</returns>
        public static bool SelectCodecs(ref string sdp, RtcCodecInfo audioCodec, RtcCodecInfo videoCodec)
        {
            Regex mfdRegex = new Regex("\r\nm=audio.*RTP.*?( .\\d*)+\r\n");
            Match mfdMatch = mfdRegex.Match(sdp);
            List<string> mfdListToErase = new List<string>(); //mdf = media format descriptor
            bool audioMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match
            if (audioMediaDescFound)
            {
                if (audioCodec == null)
                {
                    return false;
                }
                for (int groupCtr = 1/*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (int captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        mfdListToErase.Add(mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart());
                    }
                }
                if (!mfdListToErase.Remove(audioCodec.Id.ToString()))
                {
                    return false;
                }
            }

            mfdRegex = new Regex("\r\nm=video.*RTP.*?( .\\d*)+\r\n");
            mfdMatch = mfdRegex.Match(sdp);
            bool videoMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match
            if (videoMediaDescFound)
            {
                if (videoCodec == null)
                {
                    return false;
                }
                for (int groupCtr = 1/*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (int captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        mfdListToErase.Add(mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart());
                    }
                }
                if (!mfdListToErase.Remove(videoCodec.Id.ToString()))
                {
                    return false;
                }
            }

            if (audioMediaDescFound)
            {
                // Alter audio entry
                Regex audioRegex = new Regex("\r\n(m=audio.*RTP.*?)( .\\d*)+");
                sdp = audioRegex.Replace(sdp, "\r\n$1 " + audioCodec.Id);
            }

            if (videoMediaDescFound)
            {
                // Alter video entry
                Regex videoRegex = new Regex("\r\n(m=video.*RTP.*?)( .\\d*)+");
                sdp = videoRegex.Replace(sdp, "\r\n$1 " + videoCodec.Id);
            }

            // Remove associated rtp mapping, format parameters, feedback parameters
            Regex removeOtherMdfs = new Regex("a=(rtpmap|fmtp|rtcp-fb):(" + String.Join("|", mfdListToErase) + ") .*\r\n");
            sdp = removeOtherMdfs.Replace(sdp, "");

            return true;
        }
    }
}
