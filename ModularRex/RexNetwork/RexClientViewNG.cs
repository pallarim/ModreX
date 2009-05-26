using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using ModularRex.RexFramework;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.ClientStack;

namespace ModularRex.RexNetwork
{
    //TODO: RENAME!
    public class RexClientViewNG : RexClientViewBase
    {
        public RexClientViewNG(EndPoint remoteEP, IScene scene, IAssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, assetCache, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
        }

        public override void SendRexObjectProperties(UUID id, RexObjectProperties x)
        {
            //TODO: implement
        }
    }
}
