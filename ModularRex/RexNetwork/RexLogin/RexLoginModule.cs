using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexLoginModule : IRegionModule 
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<Scene> m_scenes = new List<Scene>();
        private Dictionary<UUID, string> m_authUrl = new Dictionary<UUID, string>();

        private RexUDPServer m_udpserver;
        private IConfigSource m_config;

        private RegionInfo m_primaryRegionInfo;
        private uint m_rexPort = 7000;

        public void Initialise(Scene scene, IConfigSource source)
        {
            if (m_udpserver == null)
                m_udpserver = new RexUDPServer();

            m_config = source;

            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
        }

        /// <summary>
        /// Used to transmit in the Account Name to the new RexClientView
        /// Would like to pipe it via the RexClientView constructor,
        /// but doing so would require a stactic dictionary of expected
        /// values.
        /// </summary>
        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            RexClientView rex;
            if(client.TryGet(out rex))
            {
                rex.RexAuthURL = m_authUrl[rex.AgentId];
            }
        }


        public void PostInitialise()
        {
            m_log.Info("[REX] Overloading Login_to_Simulator");
            m_scenes[0].AddXmlRPCHandler("login_to_simulator", XmlRpcLoginMethod);

            m_primaryRegionInfo = m_scenes[0].RegionInfo;

            m_log.Info("[REX] Initialising");
            m_udpserver.Initialise(m_primaryRegionInfo.ExternalEndPoint.Address, ref m_rexPort, 0, false, m_config, m_scenes[0].AssetCache,
                                   m_scenes[0].AuthenticateHandler);
            foreach (Scene scene in m_scenes)
            {
                m_udpserver.AddScene(scene);
            }
            m_udpserver.Start();


        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RexLoginOverrider"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #region RexLoginHelper

        public bool AuthenticateUser(string accountName, string sessionHash)
        {
            return true;
        }

        public virtual XmlRpcResponse XmlRpcLoginMethod(XmlRpcRequest request)
        {
            //CFK: CustomizeResponse contains sufficient strings to alleviate the need for this.
            //CKF: m_log.Info("[LOGIN]: Attempting login now...");
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable requestData = (Hashtable)request.Params[0];

            bool GoodXML = (requestData.Contains("account") && requestData.Contains("sessionhash"));
            bool GoodLogin;

            string startLocationRequest = "last";

            LoginResponse logResponse = new LoginResponse();

            string account;
            string sessionHash;

            if (GoodXML)
            {
                account = (string)requestData["account"];
                sessionHash = (string)requestData["sessionhash"];

                m_log.InfoFormat(
                    "[REX LOGIN BEGIN]: XMLRPC Received login request message from user '{0}' '{1}'",
                    account, sessionHash);

                string clientVersion = "Unknown";

                if (requestData.Contains("version"))
                {
                    clientVersion = (string)requestData["version"];
                }

                if (requestData.Contains("start"))
                {
                    startLocationRequest = (string)requestData["start"];
                }

                m_log.DebugFormat(
                    "[REXLOGIN]: XMLRPC Client is {0}, start location is {1}", clientVersion, startLocationRequest);

                GoodLogin = AuthenticateUser(account, sessionHash);
            }
            else
            {
                m_log.Info(
                    "[REXLOGIN END]: XMLRPC  login_to_simulator login message did not contain all the required data");

                return logResponse.CreateGridErrorResponse();
            }

            if (!GoodLogin)
            {
                m_log.InfoFormat("[LOGIN END]: XMLRPC  User {0} ({1}) failed authentication", account, sessionHash);

                return logResponse.CreateLoginFailedResponse();
            }
            try
            {
                
                // Inventory
                UUID invTemp = UUID.Random();
                Hashtable TempHash = new Hashtable();
                TempHash["name"] = "Root Folder";
                TempHash["parent_id"] = UUID.Zero.ToString();
                TempHash["version"] = 0;
                TempHash["type_default"] = 0;
                TempHash["folder_id"] = invTemp.ToString();


                ArrayList AgentInventoryArray = new ArrayList();
                AgentInventoryArray.Add(TempHash);

                Hashtable InventoryRootHash = new Hashtable();
                InventoryRootHash["folder_id"] = invTemp.ToString();
                ArrayList InventoryRoot = new ArrayList();
                InventoryRoot.Add(InventoryRootHash);
                /*
                // Inventory Library Section
                Hashtable InventoryLibRootHash = new Hashtable();
                InventoryLibRootHash["folder_id"] = "00000112-000f-0000-0000-000100bba000";
                ArrayList InventoryLibRoot = new ArrayList();
                InventoryLibRoot.Add(InventoryLibRootHash);

                logResponse.InventoryLibRoot = InventoryLibRoot;
                */
                logResponse.InventoryRoot = InventoryRoot;
                logResponse.InventorySkeleton = AgentInventoryArray;
                // End Inventory
                


                UUID agentID = GetAgentID(account);

                // Used to transmit the login URL to the 
                // RexAvatar class when it connects.
                m_authUrl[agentID] = account;

                logResponse.CircuitCode = Util.RandomClass.Next();



                logResponse.Lastname = "aka " + account;
                logResponse.Firstname = "Rex User";

                

                logResponse.AgentID = agentID;
                logResponse.SessionID = GetSessionID(account);
                logResponse.SecureSessionID = GetSecureID(account);
                logResponse.Message = "Welcome to ModularRex";

                logResponse.SimAddress = m_primaryRegionInfo.ExternalEndPoint.Address.ToString();
                logResponse.SimPort = m_rexPort;
                logResponse.RegionX = m_primaryRegionInfo.RegionLocX;
                logResponse.RegionY = m_primaryRegionInfo.RegionLocY;


                logResponse.StartLocation = startLocationRequest;

                string capsPath = Util.GetRandomCapsPath();
                string seedcap = "http://" + m_scenes[0].RegionInfo.ExternalEndPoint.Address + ":" +
                                 "9000" + "/CAPS/" + capsPath + "0000/";

                logResponse.SeedCapability = seedcap;

                foreach (Scene scene in m_scenes)
                {
                    AgentCircuitData acd = new AgentCircuitData();

                    acd.AgentID = agentID;
                    acd.BaseFolder = UUID.Zero;
                    acd.CapsPath = seedcap;

                    // Will login to the first region
                    acd.child = scene == m_scenes[0];

                    acd.circuitcode = (uint)logResponse.CircuitCode;
                    acd.firstname = logResponse.Firstname;
                    acd.InventoryFolder = UUID.Zero;
                    acd.lastname = logResponse.Lastname;
                    acd.SecureSessionID = logResponse.SecureSessionID;
                    acd.SessionID = logResponse.SessionID;
                    acd.startpos = new Vector3(128, 128, 128);

                    scene.NewUserConnection(acd);
                }

                

                XmlRpcResponse rep = logResponse.ToXmlRpcResponse();

                Hashtable val = (Hashtable) rep.Value;
                val["rex"] = "running rex mode";

                m_log.Debug(rep.ToString());

                return rep;
            }
            catch (Exception e)
            {
                m_log.Info("[REXLOGIN END]:  XMLRPC Login failed, " + e);
            }

            m_log.Info("[REXLOGIN END]:  XMLRPC Login failed.  Sending back blank XMLRPC response");
            return response;
        }

        private static UUID GetAgentID(string account)
        {
            UUID agentID = new UUID(Util.Md5Hash(account));
            return agentID;
        }

        private static UUID GetSessionID(string account)
        {
            UUID agentID = new UUID(Util.Md5Hash(account + "session"));
            return agentID;
        }

        /// <summary>
        /// Not really secure.
        /// </summary>
        private static UUID GetSecureID(string account)
        {
            UUID agentID = new UUID(Util.Md5Hash(account + "secure"));
            return agentID;
        }

        #endregion

    }
}