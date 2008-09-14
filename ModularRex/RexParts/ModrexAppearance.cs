using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using log4net;
using ModularRex.RexNetwork;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexParts
{
    class ModrexAppearance : IRegionModule
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string m_rexAuthServer = "http://127.0.0.1:8002";
        private Queue<XmlRpcRequest> m_reqAuthQueue = new Queue<XmlRpcRequest>();
        private Timer m_reqAuthTimer = new Timer(1500);

        private List<Scene> m_scenes = new List<Scene>();

        public void RequestRexAuthentication(UUID avatarID, string authAddress)
        {
            Hashtable ReqVals = new Hashtable();
            ReqVals["avatar_uuid"] = avatarID.ToString();
            ReqVals["AuthenticationAddress"] = authAddress;

            ArrayList SendParams = new ArrayList();
            SendParams.Add(ReqVals);

            XmlRpcRequest RexReq = new XmlRpcRequest("get_user_by_uuid", SendParams);

            lock(m_reqAuthQueue)
            {
                m_reqAuthQueue.Enqueue(RexReq);
            }

            m_reqAuthTimer.AutoReset = false;
            m_reqAuthTimer.Stop();
            m_reqAuthTimer.Start();
        }

        public void SendAppearanceToAllUsers(UUID user, string avatarServerURL)
        {
            foreach (Scene scene in m_scenes)
            {
                scene.ForEachScenePresence(
                    delegate(ScenePresence avatar)
                        {
                            if (avatar.ControllingClient is RexClientView)
                            {
                                ((RexClientView) avatar.ControllingClient).SendRexAppearance(
                                    user, avatarServerURL);
                            }
                        });
            }
        }



        public void Initialise(Scene scene, IConfigSource source)
        {
            try
            {
                if(!source.Configs["realXtend"].GetBoolean("enabled",false))
                {
                    return;
                }

                m_rexAuthServer = source.Configs["realXtend"].GetString("authentication_server",
                                                                        m_rexAuthServer);
                m_log.Info("RexAppearance Module Being Used");
            }
            catch (Exception)
            {
                m_log.Info("Rex Config Error, Disabled");
                return;
            }


            m_scenes.Add(scene);

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
            m_reqAuthTimer.Elapsed += m_reqAuthTimer_Elapsed;
        }

        void m_reqAuthTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            while(m_reqAuthQueue.Count > 0)
            {
                XmlRpcRequest req; 
                lock(m_reqAuthQueue)
                {
                    req = m_reqAuthQueue.Dequeue();
                }


                XmlRpcResponse authreply = req.Send(m_rexAuthServer, 9000);
                string rexAsAddress = ((Hashtable) authreply.Value)["as_address"].ToString();
                //string rexSkypeURL = ((Hashtable) authreply.Value)["skype_url"].ToString();
                UUID userID = ((Hashtable) authreply.Value)["uuid"].ToString();

                SendAppearanceToAllUsers(userID, rexAsAddress);
            }
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