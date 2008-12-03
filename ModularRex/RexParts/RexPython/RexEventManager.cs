using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts.RexPython
{
    [Serializable]
    class RexEventManager
    {
        private RexScriptEngine myScriptEngine;

        // tuco fixme, is there a better way to do this search???
        private EntityBase GetEntityBase(uint vId)
        {
            SceneObjectPart part = myScriptEngine.World.GetSceneObjectPart(vId);
            if (part != null && (EntityBase)(part.ParentGroup) != null)
                return (EntityBase)(part.ParentGroup);
            else              
                return null;
        }

        public RexEventManager(RexScriptEngine vScriptEngine)
        {   
            myScriptEngine = vScriptEngine;
            myScriptEngine.Log.InfoFormat("[RexScriptEngine]: Hooking up to server events");
            myScriptEngine.World.EventManager.OnObjectGrab += touch_start;
            // myScriptEngine.World.EventManager.OnRezScript += OnRezScript;
            // myScriptEngine.World.EventManager.OnRemoveScript += OnRemoveScript;
            // myScriptEngine.World.EventManager.OnFrame += OnFrame;
            //myScriptEngine.World.EventManager.OnNewClient += OnNewClient; 
            myScriptEngine.World.EventManager.OnNewPresence += OnNewPresence;
            myScriptEngine.World.EventManager.OnRemovePresence += OnRemovePresence;
            myScriptEngine.World.EventManager.OnShutdown += OnShutDown;

            ///TODO:
            ///These events were added to forked version. Some of them can be handled 
            ///other way, some need changes to core and some need changes to physics engine.
            //myScriptEngine.World.EventManager.OnAddEntity += OnAddEntity;
            //myScriptEngine.World.EventManager.OnRemoveEntity += OnRemoveEntity;
            //myScriptEngine.World.EventManager.OnPythonClassChange += OnPythonClassChange;
            //myScriptEngine.World.EventManager.OnPrimVolumeCollision += OnPrimVolumeCollision;
            //myScriptEngine.World.EventManager.OnRexScriptListen += OnRexScriptListen;  
            OpenSim.OpenSim.RegisterCmd("python", PythonScriptCommand, "Rex python commands. Type \"python help\" for more information.");
        }

        private void PythonScriptCommand(string[] cmdparams)
        {
            try
            {
                if (cmdparams.Length >= 1)
                {
                    string command = cmdparams[0].ToLower();
                    switch (command)
                    {
                        case "help":
                            myScriptEngine.Log.Info("[RexScriptEngine]: Python commands available:");
                            myScriptEngine.Log.Info("[RexScriptEngine]:    python restart - restarts the python engine");
                            break;
                        case "restart":
                            myScriptEngine.RestartPythonEngine();
                            break;                      
                        default:
                            myScriptEngine.Log.WarnFormat("[RexScriptEngine]: Unknown PythonScriptEngine command:" + cmdparams[0]);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnPythonScriptCommand: " + e.ToString());
            }
        }

        public void touch_start(uint localID, uint originalID, Vector3 offsetPos, IClientAPI remoteClient)//(uint localID, Vector3 offsetPos, IClientAPI remoteClient)
        {
            string EventParams = "\"touch_start\"," + localID.ToString() + "," + "\"" + remoteClient.AgentId.ToString() + "\"";
            myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
        }

        /* 
        public void OnRezScript(uint localID, UUID itemID, string script)
        {

        }
        public void OnRemoveScript(uint localID, UUID itemID)
        {         

        }

        public void OnFrame()
        {

        }

        public void OnNewClient(IClientAPI vClient)
        {
            string EventParams = "\"new_client\"," + "\"" + vClient.AgentId.ToString() + "\"";
            myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
        }
        */ 

        public void OnNewPresence(ScenePresence vPresence)
        {
            try
            {      
                //IRexBot related
                if (vPresence.ControllingClient is IRexBot)
                {
                    string EventParams = "\"add_bot\"," + vPresence.LocalId.ToString() + "," + "\"" + vPresence.UUID.ToString() + "\"";
                    myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");            
                }
                else    
                {
                    string EventParams = "\"add_presence\"," + vPresence.LocalId.ToString() + "," + "\"" + vPresence.UUID.ToString() + "\"";
                    myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
                }

                //Tie up some RexClientView events
                RexNetwork.RexClientView rex;
                if (vPresence.ClientView.TryGet(out rex))
                {
                    rex.OnReceiveRexStartUp += OnRexClientStartUp;
                    rex.OnReceiveRexClientScriptCmd += OnRexClientScriptCommand;
                }
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnNewPresence: " + e.ToString());
            }
        }

        public void OnRemovePresence(UUID uuid)
        {
            try
            {
                // python handles if this presence was bot or human
                string EventParams = "\"remove_presence\"," + "\"" + uuid.ToString() + "\"";
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnRemovePresence: " + e.ToString());
            }
        }

        public void OnShutDown()
        {
            Console.WriteLine("REX OnShutDown");
        }

        public void OnAddEntity(uint localID)
        {
            try
            {
                string PythonClassName = "rxactor.Actor";
                string PythonTag = "";

                RexObjects.RexObjectGroup tempobj = (RexObjects.RexObjectGroup)GetEntityBase(localID);
                //SceneObjectGroup tempobj = (SceneObjectGroup)GetEntityBase(localID);
                if (tempobj != null && tempobj.RootPart != null && tempobj.RootPart.RexClassName.Length > 0)
                    PythonClassName = tempobj.RootPart.RexClassName;

                // Create the actor directly without using an event.
                myScriptEngine.CreateActorToPython(localID.ToString(), PythonClassName, PythonTag);
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnAddEntity: " + e.ToString());
            }
        }

        public void OnRemoveEntity(uint localID)
        {
            try
            {
                string EventParams = "\"remove_entity\"," + "\"" + localID.ToString() + "\"";
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnRemoveEntity: " + e.ToString());
            }
        }

        public void OnPythonClassChange(uint localID)
        {
            try
            {
                string PythonClassName = "rxactor.Actor";
                string PythonTag = "";

                RexObjects.RexObjectGroup tempobj = (RexObjects.RexObjectGroup)GetEntityBase(localID);
                //SceneObjectGroup tempobj = (SceneObjectGroup)GetEntityBase(localID);
                if (tempobj != null && tempobj.RootPart != null && tempobj.RootPart.RexClassName.Length > 0)
                {
                    int tagindex = tempobj.RootPart.RexClassName.IndexOf("?", 0);
                    if (tagindex > -1)
                    {
                        PythonClassName = tempobj.RootPart.RexClassName.Substring(0, tagindex);
                        PythonTag = tempobj.RootPart.RexClassName.Substring(tagindex + 1);
                    }
                    else
                        PythonClassName = tempobj.RootPart.RexClassName;
                }
                if (myScriptEngine.IsEngineStarted)
                    myScriptEngine.CreateActorToPython(localID.ToString(), PythonClassName, PythonTag);
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnPythonClassChange: " + e.ToString());
            }
        }

        public void OnRexClientScriptCommand(RexNetwork.RexClientView remoteClient, UUID agentID, List<string> commands)
        {
            try
            {
                string Paramlist = "";
                foreach (string s in commands)
                    Paramlist = Paramlist + "," + "\"" + s + "\"";

                string EventParams = "\"client_event\",\"" + agentID.ToString() + "\"" + Paramlist;
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnRexClientScriptCommand: " + e.ToString());
            }
        }

        public void OnPrimVolumeCollision(uint ownID, uint colliderID)
        {
            try
            {
                string EventParams = "\"primvol_col\"," + ownID.ToString() + "," + "\"" + colliderID.ToString() + "\"";
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnPrimVolumeCollision: " + e.ToString());
            }
        }
        
        public void OnRexScriptListen(uint vPrimLocalId, int vChannel, string vName, UUID vId, string vMessage)
        {
            try
            {
                SceneObjectPart source = null;
                ScenePresence avatar = null;
                string sid = "0";

                source = myScriptEngine.World.GetSceneObjectPart(vId);
                if (source != null)
                {
                    if ((EntityBase)(source.ParentGroup) != null)
                        sid = ((EntityBase)source.ParentGroup).LocalId.ToString(); 
                }
                else
                {
                    avatar = myScriptEngine.World.GetScenePresence(vId);
                    if(avatar != null)
                        sid = avatar.UUID.ToString();
                }
            
                string EventParams = "\"listen\"," + vPrimLocalId.ToString() + "," + vChannel.ToString() + "," +
                    "\"" + vName + "\"" + "," + "\"" + sid + "\"" + "," + "\"" + vMessage.ToString() + "\"";
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnRexScriptListen: " + e.ToString());
            }
        }

        public void OnRexClientStartUp(RexNetwork.RexClientView client, UUID agentID, string status)
        {
            try
            {
                string EventParams = "\"client_startup\",\"" + agentID.ToString() + "\",\"" + status + "\"";
                myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                myScriptEngine.Log.WarnFormat("[RexScriptEngine]: OnRexClientStartUp: " + e.ToString());
            }
        }




        // TODO: Replace placeholders below
        //  These needs to be hooked up to OpenSim during init of this class.
        // When queued in EventQueueManager they need to be LSL compatible (name and params)

        //public void state_entry() { } // 
        public void state_exit() { }
        //public void touch_start() { }
        public void touch() { }
        public void touch_end() { }
        public void collision_start() { }
        public void collision() { }
        public void collision_end() { }
        public void land_collision_start() { }
        public void land_collision() { }
        public void land_collision_end() { }
        public void timer() { }
        public void listen() { }
        public void on_rez() { }
        public void sensor() { }
        public void no_sensor() { }
        public void control() { }
        public void money() { }
        public void email() { }
        public void at_target() { }
        public void not_at_target() { }
        public void at_rot_target() { }
        public void not_at_rot_target() { }
        public void run_time_permissions() { }
        public void changed() { }
        public void attach() { }
        public void dataserver() { }
        public void link_message() { }
        public void moving_start() { }
        public void moving_end() { }
        public void object_rez() { }
        public void remote_data() { }
        public void http_response() { }

    }


}
