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
        private bool windToggle = true;

        public void Initialise(Scene scene, IConfigSource source)
        {
            scene.EventManager.OnNewClient += new EventManager.OnNewClientDelegate(EventManager_OnNewClient);
        }

        void EventManager_OnNewClient(OpenSim.Framework.IClientAPI client)
        {
            client.OnChatFromClient += new OpenSim.Framework.ChatMessage(client_OnChatFromClient);
            
            if(client is RexClientView)
            {
                ((RexClientView)client).OnRexAvatarProperties += new RexAvatarPropertiesDelegate(RexScriptTestModule_OnRexAvatarProperties);
            }
        }

        void RexScriptTestModule_OnRexAvatarProperties(RexClientView sender, List<string> parameters)
        {
            sender.SendRexInventoryMessage(parameters[0]);
        }

        void client_OnChatFromClient(object sender, OpenSim.Framework.OSChatMessage e)
        {
            if (e.Message != "")
            {
                switch (e.Message.Split(' ')[0])
                {
                    case "fog":
                        if (e.Sender is RexClientView)
                        {
                            ((RexClientView)e.Sender).SendRexFog(0, 50, 50, 50, 50);
                        }
                        break;
                    case "water":
                        if (e.Sender is RexClientView)
                        {
                            if (e.Message.Split(' ').Length > 1)
                            {
                                ((RexClientView)e.Sender).SendRexWaterHeight(Convert.ToSingle(e.Message.Split(' ')[1]));
                            }
                            else
                            {
                                ((RexClientView)e.Sender).SendRexWaterHeight(50);
                            }
                        }
                        break;
                    case "postp":
                        if (e.Sender is RexClientView)
                        {
                            if (e.Message.Split(' ').Length > 2)
                            {
                                bool toggle = Convert.ToBoolean(e.Message.Split(' ')[2]);
                                int id = Convert.ToInt32(e.Message.Split(' ')[1]);
                                ((RexClientView)e.Sender).SendRexPostProcess(id, toggle);
                            }
                        }
                        break;
                    case "wind":
                        if (e.Sender is RexClientView)
                        {
                            ((RexClientView)e.Sender).SendRexToggleWindSound(!this.windToggle);
                            windToggle = !windToggle;
                            //((RexClientView)e.Sender).SendRexScriptCommand("hud", "ShowInventoryMessage(\"wind ="+windToggle.ToString()+" \")", "");
                        }
                        break;
                    default:


                        //Test code. Not to any relese.
                        //e.Sender.SendAlertMessage("Hello there");

                        //if (e.Sender is RexClientView)
                        //{
                        //    ((RexClientView)e.Sender).SendRexScriptCommand("hud", "ShowInventoryMessage(\"Test\")", "");
                        //}
                        break;
                }
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
