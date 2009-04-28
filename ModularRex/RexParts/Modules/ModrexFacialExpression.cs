using System.Collections.Generic;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;



namespace ModularRex.RexParts.Modules
{
    public class ModrexFacialExpression : IRegionModule
    {
        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.EventManager.OnNewClient += EventManager_OnNewClient;
        }

        static void EventManager_OnNewClient(IClientAPI client)
        {
            if (client is IClientRexFaceExpression)
            {
                IClientRexFaceExpression mcv = (IClientRexFaceExpression)client;
                mcv.OnRexFaceExpression += mcv_OnRexFaceExpression;
            }
        }

        static void mcv_OnRexFaceExpression(IClientAPI sender, List<string> vParams)
        {
            // OpenSim BUG: IScene contains insufficient properties for handling agents.
            // FIXME Then return.
            Scene x = (Scene) sender.Scene;
            x.ForEachScenePresence(delegate(ScenePresence scenePresence)
                                       {
                                           IClientRexFaceExpression rexFace;
                                           if (scenePresence.ClientView.TryGet(out rexFace))
                                           {
                                               rexFace.SendRexFaceExpression(vParams);
                                           }
                                       }
                );

        }

        public void PostInitialise()
        {
            
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RealXtendFacialModule"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
