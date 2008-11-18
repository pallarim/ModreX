using System;
using System.Collections.Generic;
using System.Text;
using ModularRex.RexNetwork;
using Nini.Config;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts
{
    public class RexScriptTestModule : IRegionModule 
    {
        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
        }

        void EventManager_OnNewClient(OpenSim.Framework.IClientAPI client)
        {
            client.OnChatFromClient += new OpenSim.Framework.ChatMessage(client_OnChatFromClient);
            
            if(client is RexClientView)
            {
                ((RexClientView)client).OnRexAvatarProperties += new RexAvatarProperties(RexScriptTestModule_OnRexAvatarProperties);
            }
        }

        void RexScriptTestModule_OnRexAvatarProperties(RexClientView sender, List<string> parameters)
        {
            sender.SendRexInventoryMessage(parameters[0]);
        }

        void client_OnChatFromClient(object sender, OpenSim.Framework.OSChatMessage e)
        {
            e.Sender.SendAlertMessage("Hello there");

            if(e.Sender is RexClientView)
            {
                ((RexClientView) e.Sender).SendRexScriptCommand("hud", "ShowInventoryMessage(\"Test\")", "");
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
            get { return "Rex Script Test Module"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
