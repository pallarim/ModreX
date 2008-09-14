using System.Reflection;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

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
            if(client is RexClientView)
            {
                m_log.Info(
                    "[REXCLIENT] Confirmed connection from RexPacketServer. This user can use Rex functionalities.");
            }
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
