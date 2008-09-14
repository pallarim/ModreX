using System.Collections.Generic;
using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenMetaverse;
using OpenMetaverse.Packets;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts
{
    class ModrexAppearance : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // This is the 'cheapest' time-wise conversion of the Rex code to a module.
        // It's also the most likely to break horribly once OutPacket is depreciated
        public void SendRexAppearanceAlpha(IClientAPI user, UUID agentID, string avatarAddress) //rex
        {
            GenericMessagePacket gmp = new GenericMessagePacket();
            gmp.MethodData.Method = Utils.StringToBytes("RexAppearance");
            gmp.ParamList = new GenericMessagePacket.ParamListBlock[2];
            gmp.ParamList[0] = new GenericMessagePacket.ParamListBlock();
            gmp.ParamList[0].Parameter = Utils.StringToBytes(avatarAddress);
            gmp.ParamList[1] = new GenericMessagePacket.ParamListBlock();
            gmp.ParamList[1].Parameter = Utils.StringToBytes(agentID.ToString());
            
            user.OutPacket(gmp, ThrottleOutPacketType.Task);
        }

        // We converted the Send/Recieve GenericMessage into an IClientAPI function and event
        // these are OnGenericMessage and SendGenericMessage, however this is still
        // not ideal since we're doing "Packet Logic" in a Region Module.
        public void SendRexAppearanceBeta(IClientAPI user, UUID agentID, string avatarAddress)
        {
            List<string> pack = new List<string>();
            pack.Add(avatarAddress);
            pack.Add(agentID.ToString());

            user.SendGenericMessage("RexAppearance", pack);
        }

        // Final conversion:
        //          RealXtendClientView derives from LLClientView
        //          Implements additional SendXYZ/etc functions, and converts them
        //          to genericmessage handlers, etc.
        //
        // This requires a 'smart' incoming ClientStack listener which will substantiate
        // a RealXtendClientView instead of LLClientView when the Rex version string has
        // been encountered.
        //
        // Upsides: Very clean, allows overriding of other methods and functionality
        //          when required.
        //          We dont send Rex packets to clients which dont support them
        //          (big plus for the older packets which crash the viewer)
        //
        // Downsides: Using the Rex protocol in conjunction with another version string
        //            wont work. This is fixable in time by converting checks from 
        //            RealXtendClientView to IRexClientView (that particular mod is 
        //            relatively painless)

        public void SendRexAppearanceGamma(IClientAPI user, UUID agentID, string avatarAddress)
        {
            if(user is RexClientView)
            {
                RexClientView rex = (RexClientView) user;
                rex.SendRexAppearance(agentID, avatarAddress);
            }
        }

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        void EventManager_OnNewClient(IClientAPI client)
        {
            // Check if the client was insubstantiated as a RexClientView.
            if(client is RexClientView)
            {
                RexClientView mcv = (RexClientView) client;

                mcv.OnRexAppearance += mcv_OnRexAppearance;
            }
        }

        /// <summary>
        /// Fired when a "Neighbours: Update your appearance" packet is sent by the viewer
        /// </summary>
        /// <param name="sender">IClientApi of the sender</param>
        void mcv_OnRexAppearance(RexClientView sender)
        {
            
        }

        public void PostInitialise()
        {
            
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RealXtendAppearanceModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}