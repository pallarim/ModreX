using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using OpenSim.Framework;
using OpenMetaverse;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.ClientStack;

namespace ModularRex.RexNetwork
{
    public delegate void ReceiveRexSkypeStore(RexClientViewLegacy remoteClient);

    /// <summary>
    /// Legacy client only differs from RexClientView base by having RexSkypeUrl feature.
    /// More will will be later
    /// </summary>
    public class RexClientViewLegacy : RexClientViewBase
    {
        private string m_rexSkypeURL;

        public event ReceiveRexSkypeStore OnNewRexSkypeUrl;

        /// <summary>
        /// Skype username of the avatar
        /// eg: Skypeuser
        /// </summary>
        public string RexSkypeURL
        {
            get { return m_rexSkypeURL; }
            set
            {
                m_rexSkypeURL = value;
                if (OnNewRexSkypeUrl != null)
                {
                    OnNewRexSkypeUrl(this);
                }
            }
        }


        public RexClientViewLegacy(EndPoint remoteEP, IScene scene,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            AddGenericPacketHandler("RexSkypeStore", HandleOnSkypeStore);
        }

        private void HandleOnSkypeStore(object sender, string method, List<string> args)
        {
            if (method.ToLower() == "rexskypestore")
            {
                string skypeAddr = args[0];
                this.RexSkypeURL = skypeAddr;
            }
        }

        public void SendSkypeAddress(UUID agentID, string skypeAddress)
        {
            List<string> pack = new List<string>();

            pack.Add(skypeAddress);
            pack.Add(agentID.ToString());

            SendGenericMessage("SkypeAdderss", pack);
        }
    }
}
