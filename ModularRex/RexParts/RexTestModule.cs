using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;

namespace ModularRex.RexParts
{
    public class RexTestModule : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        void EventManager_OnNewClient(OpenSim.Framework.IClientAPI client)
        {
            if (client is RexClientView)
            {
                m_log.Info(
                    "[REXCLIENT] Confirmed connection from RexPacketServer. This user can use Rex functionalities.");

                RexClientView rcv = (RexClientView)client;
                rcv.OnRexAppearance += rcv_OnRexAppearance;
                rcv.OnRexFaceExpression += rcv_OnRexFaceExpression;
                rcv.OnChatFromClient += rcv_OnChatFromClient;
            }
            else
            {
                m_log.Info("[REXCLIENT] User is not entering via RexPacketServer. Ignoring.");
            }
        }

        void rcv_OnChatFromClient(object sender, OpenSim.Framework.OSChatMessage e)
        {
            if (e.Message.StartsWith("/rexauth "))
            {
                ((RexClientView)e.Sender).RexAuthURL = e.Message.Split(' ')[1];
            }
            if (e.Message.StartsWith("/rexav "))
            {
                ((RexClientView)e.Sender).RexAvatarURL = e.Message.Split(' ')[1];
            }
        }

        void rcv_OnRexFaceExpression(RexClientView sender, System.Collections.Generic.List<string> vParams)
        {
            m_log.Info("[REXCLIENT] Recieved Rex Facial Expression");
        }

        void rcv_OnRexAppearance(RexClientView sender)
        {
            m_log.Info("[REXCLIENT] Recieved Rex Appearance");
        }

        public void PostInitialise()
        {
            
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RexTestModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
