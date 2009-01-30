using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenSim;
using OpenSim.Framework;
using OpenSim.Region.Environment.Scenes;
using log4net;

namespace ModularRex.RexParts.RexPython
{
    [Serializable]
    class RexEventManager
    {
        private RexScriptEngine m_scriptEngine;
        private static readonly ILog m_log
            = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ModrexObjects m_rexObjects;

        // tuco fixme, is there a better way to do this search???
        private EntityBase GetEntityBase(uint vId)
        {
            SceneObjectPart part = m_scriptEngine.World.GetSceneObjectPart(vId);
            if (part != null && (EntityBase)(part.ParentGroup) != null)
                return (EntityBase)(part.ParentGroup);
            else              
                return null;
        }

        public RexEventManager(RexScriptEngine scriptEngine)
        {   
            m_scriptEngine = scriptEngine;
            m_log.InfoFormat("[RexScriptEngine]: Hooking up to server events");

            OpenSim.Region.Environment.Interfaces.IRegionModule module = m_scriptEngine.World.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
                m_rexObjects.OnPythonClassChange += onPythonClassChange;
                //myScriptEngine.World.EventManager.OnPythonClassChange += OnPythonClassChange; //this was launched from SceneObjectPart
            }

            m_scriptEngine.World.EventManager.OnObjectGrab += touch_start;
            // myScriptEngine.World.EventManager.OnRezScript += OnRezScript;
            // myScriptEngine.World.EventManager.OnRemoveScript += OnRemoveScript;
            // myScriptEngine.World.EventManager.OnFrame += OnFrame;
            m_scriptEngine.World.EventManager.OnNewClient += OnNewClient; 
            //m_scriptEngine.World.EventManager.OnNewPresence += OnNewPresence; //this ain't triggered in OpenSim no more
            m_scriptEngine.World.EventManager.OnRemovePresence += OnRemovePresence;
            m_scriptEngine.World.EventManager.OnShutdown += OnShutDown;

            ///TODO:
            ///These events were added to forked version. Some of them can be handled 
            ///other way, some need changes to core and some need changes to physics engine.
            //myScriptEngine.World.EventManager.OnAddEntity += OnAddEntity; //this was previously launched from Scene or InnerScene
            //myScriptEngine.World.EventManager.OnRemoveEntity += OnRemoveEntity; //this was previously launched from Scene
            //myScriptEngine.World.EventManager.OnPrimVolumeCollision += OnPrimVolumeCollision; //this was launched from PhysicActor
            m_scriptEngine.World.EventManager.OnChatFromWorld += OnRexScriptListen;
            m_scriptEngine.World.EventManager.OnChatBroadcast += OnRexScriptListen;
            m_scriptEngine.World.EventManager.OnChatFromClient += OnRexScriptListen;
            OpenSim.OpenSim.RegisterCmd("python", PythonScriptCommand, "Rex python commands. Type \"python help\" for more information.");
        }

        private void onPythonClassChange(UUID id)
        {
            SceneObjectPart sop = m_scriptEngine.World.GetSceneObjectPart(id);
            if (sop != null)
            {
                OnPythonClassChange(sop.LocalId);
            }
            else
            {
                m_log.Warn("[RexScriptEngine]: Scene Object Part not found. Could not initiate OnPythonClassChange");
            }
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
                            m_log.Info("[RexScriptEngine]: Python commands available:");
                            m_log.Info("[RexScriptEngine]:    python restart - restarts the python engine");
                            break;
                        case "restart":
                            m_scriptEngine.RestartPythonEngine();
                            break;                      
                        default:
                            m_log.WarnFormat("[RexScriptEngine]: Unknown PythonScriptEngine command:" + cmdparams[0]);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnPythonScriptCommand: " + e.ToString());
            }
        }

        public void touch_start(uint localID, uint originalID, Vector3 offsetPos, IClientAPI remoteClient, SurfaceTouchEventArgs surfaceArgs)//(uint localID, Vector3 offsetPos, IClientAPI remoteClient)
        {
            string EventParams = "\"touch_start\"," + localID.ToString() + "," + "\"" + remoteClient.AgentId.ToString() + "\"";
            m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
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
        */

        public void OnNewClient(IClientAPI vClient)
        {
            //string EventParams = "\"new_client\"," + "\"" + vClient.AgentId.ToString() + "\"";
            //myScriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            ScenePresence sp = m_scriptEngine.World.GetScenePresence(vClient.AgentId);
            OnNewPresence(sp);
        }
         

        public void OnNewPresence(ScenePresence vPresence)
        {
            try
            {      
                //IRexBot related
                if (vPresence.ControllingClient is IRexBot)
                {
                    string EventParams = "\"add_bot\"," + vPresence.LocalId.ToString() + "," + "\"" + vPresence.UUID.ToString() + "\"";
                    m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");            
                }
                else    
                {
                    string EventParams = "\"add_presence\"," + vPresence.LocalId.ToString() + "," + "\"" + vPresence.UUID.ToString() + "\"";
                    m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
                    m_log.Debug("[REXSCRIPT]: CreateEventWithName(" + EventParams + ")");
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
                m_log.WarnFormat("[RexScriptEngine]: OnNewPresence: " + e.ToString());
            }
        }

        public void OnRemovePresence(UUID uuid)
        {
            try
            {
                // python handles if this presence was bot or human
                string EventParams = "\"remove_presence\"," + "\"" + uuid.ToString() + "\"";
                m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnRemovePresence: " + e.ToString());
            }
        }

        public void OnShutDown()
        {
            m_log.Info("[RexScriptEngine]: REX OnShutDown");
        }

        public void OnAddEntity(uint localID)
        {
            throw new NotImplementedException("OnAddEntity not implemeted");
            //try
            //{
            //    string PythonClassName = "rxactor.Actor";
            //    string PythonTag = "";

            //    RexObjects.RexObjectGroup tempobj = (RexObjects.RexObjectGroup)GetEntityBase(localID);
            //    //SceneObjectGroup tempobj = (SceneObjectGroup)GetEntityBase(localID);
            //    if (tempobj != null && tempobj.RootPart != null && tempobj.RootPart.RexClassName.Length > 0)
            //        PythonClassName = tempobj.RootPart.RexClassName;

            //    // Create the actor directly without using an event.
            //    m_scriptEngine.CreateActorToPython(localID.ToString(), PythonClassName, PythonTag);
            //}
            //catch (Exception e)
            //{
            //    m_log.WarnFormat("[RexScriptEngine]: OnAddEntity: " + e.ToString());
            //}
        }

        public void OnRemoveEntity(uint localID)
        {
            try
            {
                string EventParams = "\"remove_entity\"," + "\"" + localID.ToString() + "\"";
                m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnRemoveEntity: " + e.ToString());
            }
        }

        public void OnPythonClassChange(uint localID)
        {
            try
            {
                string PythonClassName = "rxactor.Actor";
                string PythonTag = "";

                SceneObjectPart tempobj = m_scriptEngine.World.GetSceneObjectPart(localID);
                if (tempobj != null)
                {

                    RexFramework.RexObjectProperties rexObj = m_rexObjects.Load(tempobj.UUID);
                    if (rexObj != null)
                    {
                        int tagindex = rexObj.RexClassName.IndexOf("?", 0);
                        if (tagindex > -1)
                        {
                            PythonClassName = rexObj.RexClassName.Substring(0, tagindex);
                            PythonTag = rexObj.RexClassName.Substring(tagindex + 1);
                        }
                        else
                            PythonClassName = rexObj.RexClassName;

                        if (rexObj.RexClassName.Length > 0)
                        {
                            tempobj.SetScriptEvents(rexObj.ParentObjectID, (int)scriptEvents.touch_start);
                            tempobj.SendFullUpdateToAllClients();
                        }
                    }
                    if (m_scriptEngine.IsEngineStarted)
                        m_scriptEngine.CreateActorToPython(localID.ToString(), PythonClassName, PythonTag);
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnPythonClassChange: " + e.ToString());
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
                m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnRexClientScriptCommand: " + e.ToString());
            }
        }

        public void OnPrimVolumeCollision(uint ownID, uint colliderID)
        {
            try
            {
                string EventParams = "\"primvol_col\"," + ownID.ToString() + "," + "\"" + colliderID.ToString() + "\"";
                m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnPrimVolumeCollision: " + e.ToString());
            }
        }
        
        /// <summary>
        /// Listens to all world scripts and clients. Sends event from that to python scripts
        /// </summary>
        public void OnRexScriptListen(object sender, OSChatMessage chat)//uint vPrimLocalId, int vChannel, string vName, UUID vId, string vMessage)
        {
            try
            {
                if (chat.Message != "")
                {
                    uint localId = 0;
                    string sid = "0";
                    string name = chat.From;

                    SceneObjectPart sop = m_scriptEngine.World.GetSceneObjectPart(chat.SenderUUID);
                    if (sop != null)
                    {
                        localId = sop.LocalId;
                        sid = sop.ParentGroup.LocalId.ToString();
                    }

                    if (chat.Sender != null)
                    {
                        ScenePresence sp = m_scriptEngine.World.GetScenePresence(chat.Sender.AgentId);
                        if (sp != null)
                        {
                            localId = sp.LocalId;
                            sid = chat.Sender.AgentId.ToString();
                            if (name == "" || name == null) name = chat.Sender.Name; 
                        }
                    }

                    //if (chat.SenderObject != null)
                    //{
                    //    m_log.Info("sender is an "+chat.SenderObject.GetType());
                    //}

                    string eventParams = "\"listen\"," + localId + "," + chat.Channel.ToString() + "," +
                        "\"" + name + "\"" + "," + "\"" + sid + "\"" + "," + "\"" + chat.Message + "\"";
                    m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + eventParams + ")");
                    //m_log.Info(eventParams);
                }
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnRexScriptListen: " + e.ToString());
            }
        }

        public void OnRexClientStartUp(RexNetwork.RexClientView client, UUID agentID, string status)
        {
            try
            {
                string EventParams = "\"client_startup\",\"" + agentID.ToString() + "\",\"" + status + "\"";
                m_scriptEngine.ExecutePythonCommand("CreateEventWithName(" + EventParams + ")");
            }
            catch (Exception e)
            {
                m_log.WarnFormat("[RexScriptEngine]: OnRexClientStartUp: " + e.ToString());
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
