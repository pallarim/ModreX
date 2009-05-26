using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;
using Nwc.XmlRpc;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Services;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Framework.Servers;
using ModularRex.RexDBObjects;
using OpenSim.Framework.Servers.HttpServer;
using System.Net;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexLoginModule : IRegionModule, IRexUDPPort
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<Scene> m_scenes = new List<Scene>();
        private readonly Dictionary<UUID, RexUserProfileData> m_userData = new Dictionary<UUID, RexUserProfileData>();

        private List<RexUDPServer> m_udpservers = new List<RexUDPServer>();
        private Dictionary<ulong, int> m_region_ports = new Dictionary<ulong, int>();
        private IConfigSource m_config;

        private RegionInfo m_primaryRegionInfo;

        private XmlRpcMethod default_login_to_simulator;

        private int m_nextUdpPort = 7000;
        private bool m_checkSessionHash = true;

        /// <summary>
        /// Used during login to send the skeleton of the OpenSim Library to the client.
        /// </summary>
        protected LibraryRootFolder m_libraryRootFolder;

        protected WorldAssetsFolder m_worldAssets;

        public void Initialise(Scene scene, IConfigSource source)
        {
            m_config = source;

            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
            scene.RegisterModuleInterface<IRexUDPPort>(this);

            if (m_config.Configs["realXtend"].GetBoolean("enabled", true))
            {
                if (m_scenes.Count == 1) //Listen very carefully, I will say this only once
                {
                    //Load OpenSim Library folder config
                    string LibrariesXMLFile = m_config.Configs["StandAlone"].GetString("LibrariesXMLFile");
                    m_libraryRootFolder = new LibraryRootFolder(LibrariesXMLFile);

                    m_nextUdpPort = m_config.Configs["realXtend"].GetInt("FirstPort", 7000);

                    m_primaryRegionInfo = m_scenes[0].RegionInfo;

                    m_checkSessionHash = m_config.Configs["realXtend"].GetBoolean("CheckSessionHash", true);
                }

                m_log.Info("[REX] Initialising");

                RexUDPServer udpserver = new RexUDPServer();
                uint _udpport = (uint)m_nextUdpPort;
                udpserver.Initialise(scene.RegionInfo.InternalEndPoint.Address, ref _udpport, 0, false, m_config,
                    scene.AuthenticateHandler);
                udpserver.AddScene(scene);

                m_region_ports.Add(scene.RegionInfo.RegionHandle, m_nextUdpPort);
                m_udpservers.Add(udpserver);
                udpserver.Start();
                m_nextUdpPort++;

            }
            else
            {
                m_log.Info("[REX] Not overloading Login_to_Simulator and not starting UDP server");
            }
        }

        /// <summary>
        /// Used to transmit in the Account Name to the new RexClientView
        /// Would like to pipe it via the RexClientView constructor,
        /// but doing so would require a stactic dictionary of expected
        /// values.
        /// </summary>
        void EventManager_OnClientConnect(OpenSim.Framework.Client.IClientCore client)
        {
            RexClientViewBase rex;
            if(client.TryGet(out rex))
            {
                if (m_userData.ContainsKey(rex.AgentId))
                {
                    if (m_userData[rex.AgentId].Account.Contains("@"))
                    {
                        rex.RexAccount = m_userData[rex.AgentId].Account;
                    }
                    else
                    {
                        rex.RexAccount = m_userData[rex.AgentId].Account + "@" + m_userData[rex.AgentId].AuthUrl;
                    }
                    rex.RexAvatarURL = m_userData[rex.AgentId].AvatarStorageUrl;

                    if (rex is RexClientViewLegacy)
                    {
                        ((RexClientViewLegacy)rex).RexSkypeURL = m_userData[rex.AgentId].SkypeUrl;
                    }
                }
                else
                {
                    m_log.WarnFormat("[REX] Client {0} does not have realXtend acccount", rex.AgentId);
                }
            }
        }


        public void PostInitialise()
        {
            //Do this here because LLStandaloneLoginModule will register it's own login method in Initializion for each Scene
            m_log.Info("[REX] Overloading Login_to_Simulator");
            default_login_to_simulator = m_scenes[0].CommsManager.HttpServer.GetXmlRPCHandler("login_to_simulator");
            m_scenes[0].CommsManager.HttpServer.AddXmlRPCHandler("login_to_simulator", XmlRpcLoginMethod);

            m_worldAssets = m_scenes[0].RequestModuleInterface<WorldAssetsFolder>();
            if (m_worldAssets != null)
                m_libraryRootFolder.AddChildFolder(m_worldAssets);

            //Rex-NG
            m_scenes[0].CommsManager.HttpServer.AddHTTPHandler("/enable_client", CableBeachLoginMethod);
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

        public bool AuthenticateUser(string account, string sessionHash)
        {
            if (m_checkSessionHash)
            {
                string actName = account.Split('@')[0];
                string actSrv = account.Split('@')[1];
                return AuthenticationService.SimAuthenticationAccount(actName, sessionHash, actSrv);
            }
            else
            {
                return true;
            }
        }

        private static LoginService.InventoryData GetInventorySkeleton(Scene any, UUID userID)
        {
            List<InventoryFolderBase> folders = any.CommsManager.InterServiceInventoryService.GetInventorySkeleton(userID);

            // If we have user auth but no inventory folders for some reason, create a new set of folders.
            if (null == folders || 0 == folders.Count)
            {
                any.CommsManager.InterServiceInventoryService.CreateNewUserInventory(userID);
                folders = any.CommsManager.InterServiceInventoryService.GetInventorySkeleton(userID);
            }

            UUID rootID = UUID.Zero;
            ArrayList AgentInventoryArray = new ArrayList();
            foreach (InventoryFolderBase InvFolder in folders)
            {
                if (InvFolder.ParentID == UUID.Zero)
                {
                    rootID = InvFolder.ID;
                }
                Hashtable TempHash = new Hashtable();
                TempHash["name"] = InvFolder.Name;
                TempHash["parent_id"] = InvFolder.ParentID.ToString();
                TempHash["version"] = (Int32)InvFolder.Version;
                TempHash["type_default"] = (Int32)InvFolder.Type;
                TempHash["folder_id"] = InvFolder.ID.ToString();
                AgentInventoryArray.Add(TempHash);
            }

            return new LoginService.InventoryData(AgentInventoryArray, rootID);
        }

        private static ArrayList GetLibraryOwner()
        {
            //for now create random inventory library owner
            Hashtable TempHash = new Hashtable();
            TempHash["agent_id"] = "11111111-1111-0000-0000-000100bba000";
            ArrayList inventoryLibOwner = new ArrayList();
            inventoryLibOwner.Add(TempHash);
            return inventoryLibOwner;
        }

        public virtual XmlRpcResponse XmlRpcLoginMethod(XmlRpcRequest request, IPEndPoint client)
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
            string clientVersion = "Unknown";

            if (requestData.Contains("version"))
            {
                clientVersion = (string)requestData["version"];
            }

            if (GoodXML)
            {
                account = (string)requestData["account"];
                sessionHash = (string)requestData["sessionhash"];

                m_log.InfoFormat(
                    "[REX LOGIN BEGIN]: XMLRPC Received login request message from user '{0}' '{1}'",
                    account, sessionHash);

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
                if (clientVersion.StartsWith("realXtend"))
                {
                    XmlRpcResponse rep = default_login_to_simulator(request, client);
                    Hashtable val = (Hashtable)rep.Value;
                    val["rex"] = "running rex mode";
                    val["sim_port"] = GetPort(m_primaryRegionInfo.RegionHandle);                    
                    val["region_x"] = (Int32)(m_primaryRegionInfo.RegionLocX * 256);
                    val["region_y"] = (Int32)(m_primaryRegionInfo.RegionLocY * 256);
                    return rep;
                }
                else
                {
                    if (default_login_to_simulator != null)
                    {
                        m_log.Info(
                            "[REXLOGIN END]: XMLRPC  login_to_simulator login message did not contain all the required data. Trying default method.");
                        return default_login_to_simulator(request, client);
                    }
                    else
                    {
                        m_log.Info(
                            "[REXLOGIN END]: XMLRPC  login_to_simulator login message did not contain all the required data.");
                        return logResponse.CreateGridErrorResponse();
                    }
                }
            }

            if (!GoodLogin)
            {
                m_log.InfoFormat("[LOGIN END]: XMLRPC  User {0} ({1}) failed authentication", account, sessionHash);

                return logResponse.CreateLoginFailedResponse();
            }
            try
            {
                string actName = account.Split('@')[0];
                string actSrv = account.Split('@')[1];

                RexUserProfileData rap = AuthenticationService.GetUserByAccount(actName, actSrv);
                //RexAccountProperties rap = GetRexProperties(actName, actSrv);

                UUID agentID = rap.ID;

                // Used to transmit the login URL to the 
                // RexAvatar class when it connects.
                m_userData[agentID] = rap;

                logResponse.CircuitCode = Util.RandomClass.Next();

                logResponse.Lastname = "<" + account + ">";
                logResponse.Firstname = rap.FirstName + " " + rap.SurName;

                logResponse.AgentID = agentID;

                logResponse.Message = "Welcome to ModularRex";

                logResponse.SimAddress = m_primaryRegionInfo.ExternalEndPoint.Address.ToString();
                logResponse.SimPort = (uint)GetPort(m_primaryRegionInfo.RegionHandle);
                logResponse.RegionX = m_primaryRegionInfo.RegionLocX;
                logResponse.RegionY = m_primaryRegionInfo.RegionLocY;


                logResponse.StartLocation = startLocationRequest;

                string capsPath = OpenSim.Framework.Communications.Capabilities.CapsUtil.GetRandomCapsObjectPath();
                string httpServerURI = "http://" + m_primaryRegionInfo.ExternalHostName + ":" + m_primaryRegionInfo.HttpPort;
                //string seedcap = "http://" + m_scenes[0].RegionInfo.ExternalEndPoint.Address + ":" +
                //                 "9000" + "/CAPS/" + capsPath + "0000/";
                string seedcap = httpServerURI + OpenSim.Framework.Communications.Capabilities.CapsUtil.GetCapsSeedPath(capsPath);//capsPath;// + "0000/";

                logResponse.SeedCapability = seedcap;


                //UserAdminService is null in grid mode
                m_scenes[0].CommsManager.UserAdminService.AddUser(logResponse.Firstname, logResponse.Lastname, "",
                                                                      account, 1000, 1000, agentID);

                UserProfileData user = m_scenes[0].CommsManager.UserService.GetUserProfile(agentID);
                if (m_scenes[0].CommsManager.UserService is UserManagerBase)
                {
                    ((UserManagerBase)m_scenes[0].CommsManager.UserService).CreateAgent(user, request);
                    ((UserManagerBase)m_scenes[0].CommsManager.UserService).CommitAgent(ref user);
                }

                if (user.CurrentAgent != null)
                {
                    logResponse.SessionID = user.CurrentAgent.SessionID;
                    logResponse.SecureSessionID = user.CurrentAgent.SecureSessionID;
                }
                else
                {
                    // NOT SECURE
                    logResponse.SessionID = GetSessionID(account);
                    logResponse.SecureSessionID = GetSecureID(account);
                }

                m_scenes[0].CommsManager.InterServiceInventoryService.CreateNewUserInventory(agentID);
                
                LoginService.InventoryData inventData = GetInventorySkeleton(m_scenes[0], agentID);

                ArrayList AgentInventoryArray = inventData.InventoryArray;

                Hashtable InventoryRootHash = new Hashtable();
                InventoryRootHash["folder_id"] = inventData.RootFolderID.ToString();
                ArrayList InventoryRoot = new ArrayList();
                InventoryRoot.Add(InventoryRootHash);
                //userProfile.RootInventoryFolderID = inventData.RootFolderID;

                // Inventory Library Section
                Hashtable InventoryLibRootHash = new Hashtable();
                InventoryLibRootHash["folder_id"] = "00000112-000f-0000-0000-000100bba000";
                ArrayList InventoryLibRoot = new ArrayList();
                InventoryLibRoot.Add(InventoryLibRootHash);

                logResponse.InventoryLibRoot = InventoryLibRoot;
                logResponse.InventoryLibraryOwner = GetLibraryOwner();
                logResponse.InventoryRoot = InventoryRoot;
                logResponse.InventorySkeleton = AgentInventoryArray;
                logResponse.InventoryLibrary = GetInventoryLibrary();

                foreach (Scene scene in m_scenes)
                {
                    AgentCircuitData acd = new AgentCircuitData();

                    acd.AgentID = agentID;
                    acd.BaseFolder = UUID.Zero;
                    acd.CapsPath = capsPath;// seedcap; //this was causing problems

                    // Will login to the first region
                    acd.child = scene == m_scenes[0];

                    acd.circuitcode = (uint)logResponse.CircuitCode;
                    acd.firstname = logResponse.Firstname;
                    acd.InventoryFolder = UUID.Zero;
                    acd.lastname = logResponse.Lastname;
                    acd.SecureSessionID = logResponse.SecureSessionID;
                    acd.SessionID = logResponse.SessionID;
                    acd.startpos = new Vector3(128, 128, 128);

                    string reason;

                    if (!scene.NewUserConnection(acd, out reason))
                    {
                        //Login failed
                        XmlRpcResponse resp = new XmlRpcResponse();
                        Hashtable respdata = new Hashtable();
                        respdata["success"] = "FALSE";
                        respdata["reason"] = reason;
                        resp.Value = respdata;
                        return resp;
                    }

                }

                XmlRpcResponse rep = logResponse.ToXmlRpcResponse();

                Hashtable val = (Hashtable) rep.Value;
                val["rex"] = "running rex mode";

                //m_log.Debug(rep.ToString());

                return rep;
            }
            catch (Exception e)
            {
                m_log.Info("[REXLOGIN END]:  XMLRPC Login failed, " + e);
            }

            m_log.Info("[REXLOGIN END]:  XMLRPC Login failed.  Sending back blank XMLRPC response");
            return response;
        }

        //Rex-NG
        /// <summary>
        /// Received from the user server when a user starts logging in.  This call allows
        /// the region to prepare for direct communication from the client.  Sends back an empty
        /// xmlrpc response on completion.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// 

        public Hashtable CableBeachLoginMethod(Hashtable requestData)
        {

            OSDMap args = OpenSim.Framework.Communications.Clients.RegionClient.GetOSDMap((string)requestData["body"]);            
            string clientVersion = "Unknown";
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable respdata = new Hashtable();

            AgentCircuitData agentData = new AgentCircuitData();
            agentData.SessionID = new UUID((string)args["session_id"].AsString());
            agentData.SecureSessionID = new UUID((string)args["secure_session_id"].AsString());
            agentData.AgentID = new UUID((string)args["agent_id"].AsString());
            agentData.circuitcode = Convert.ToUInt32(args["circuit_code"].AsString());
            agentData.CapsPath = args["caps_path"].AsString();            
            OSDMap attributes = (OSDMap)args["attributes"];
            OSDArray regionHandleArray = (OSDArray)args["region_handle"];

            if (args["attributes"] != null)
                agentData.firstname = attributes["http://axschema.org/namePerson/first"].AsString();
                agentData.lastname = attributes["http://axschema.org/namePerson/last"].AsString();


            if (args["client_version"] != null)
            {
                clientVersion = args["client_version"].AsString();
            }


            try
            {

                RexUserProfileData rap = new RexUserProfileData();
                rap.ID = agentData.AgentID;
                rap.CurrentAgent = new UserAgentData();
                rap.CurrentAgent.ProfileID = agentData.AgentID;

                //Added to fix OnClientConnect event above. 
                rap.AuthUrl = "http://localhost:8002";
                rap.Account = agentData.firstname + "_" + agentData.lastname;


                rap.FirstName = agentData.firstname;                
                rap.SurName = agentData.lastname;                
                rap.CurrentAgent.SecureSessionID = agentData.SecureSessionID;
                rap.CurrentAgent.SessionID = agentData.SessionID;
                rap.CurrentAgent.Position = agentData.startpos;

                //TODO RexUserProfileData is not complete. Do we actually need it? 
                //In case we do, the cable beach enable_client must provide all needed data

                // Used to transmit the login URL to the 
                // RexAvatar class when it connects.
                m_userData[agentData.AgentID] = rap;

                string capsPath = OpenSim.Framework.Communications.Capabilities.CapsUtil.GetRandomCapsObjectPath();
                string httpServerURI = "http://" + m_primaryRegionInfo.ExternalHostName + ":" + m_primaryRegionInfo.HttpPort;
                string seedcap = httpServerURI + OpenSim.Framework.Communications.Capabilities.CapsUtil.GetCapsSeedPath(capsPath);//capsPath;// + "0000/";

                //UserAdminService is null in grid mode
                //m_scenes[0].CommsManager.UserAdminService.AddUser(agentData.firstname, agentData.lastname, "",
                //                                                      "", 1000, 1000, agentData.AgentID);

                if (m_scenes[0].CommsManager.UserService != null)
                {
                    UserProfileData user = m_scenes[0].CommsManager.UserService.GetUserProfile(agentData.AgentID);
                    if (m_scenes[0].CommsManager.UserService is UserManagerBase)
                    {
                        ((UserManagerBase)m_scenes[0].CommsManager.UserService).CreateAgent(user, args);
                        ((UserManagerBase)m_scenes[0].CommsManager.UserService).CommitAgent(ref user);
                    }
                }

                if (m_scenes[0].CommsManager.InterServiceInventoryService != null)
                {
                    m_scenes[0].CommsManager.InterServiceInventoryService.CreateNewUserInventory(agentData.AgentID);
                }

                foreach (Scene scene in m_scenes)
                {
                    AgentCircuitData acd = new AgentCircuitData();

                    acd.AgentID = agentData.AgentID;
                    acd.BaseFolder = UUID.Zero;
                    acd.CapsPath = capsPath;
                    acd.child = scene == m_scenes[0];                    
                    acd.circuitcode = (uint)agentData.circuitcode;
                    acd.firstname = agentData.firstname;
                    acd.InventoryFolder = UUID.Zero;
                    acd.lastname = agentData.lastname;
                    acd.SecureSessionID = agentData.SecureSessionID;
                    acd.SessionID = agentData.SessionID;
                    acd.startpos = new Vector3(128, 128, 128);

                    string reason;

                    if (!scene.NewUserConnection(acd, out reason))
                    {
                        //Login failed                        
                        respdata["content_type"] = "application/json";
                        respdata["int_response_code"] = 500;
                        respdata["success"] = "FALSE";
                        respdata["reason"] = "Error adding new agent to scene.";
                        respdata["str_response_string"] = "Error adding new agent to scene.";
                        return respdata;
                    }

                }

                respdata["content_type"] = "application/json";
                respdata["int_response_code"] = 200;
                respdata["success"] = "TRUE";

                OSDMap map = new OSDMap();
                map.Add("seed_capability", new OSDURI(new Uri(seedcap)));
                map.Add("sim_port", new OSDInteger(GetPort(m_primaryRegionInfo.RegionHandle)));
                map.Add("sim_address", new OSDString(m_primaryRegionInfo.ExternalEndPoint.Address.ToString()));
                string strBuffer = "";
                try
                {
                    strBuffer = OSDParser.SerializeJsonString(map);
                }
                catch (Exception e)
                {
                    m_log.WarnFormat("[CABLE BEACH REX LOGIN]: Exception thrown on serialization of CreateObject: {0}", e.Message);                    
                }

                respdata["str_response_string"] = strBuffer;

                return respdata;
            }
            catch (Exception e)
            {
                m_log.Info("[CABLE BEACH REXLOGIN END]:  Cable Beach Login failed, " + e);
            }

            m_log.Info("[CABLE BEACH REXLOGIN END]:  Cable Beach login failed.");

            respdata["content_type"] = "application/json";
            respdata["int_response_code"] = 500;
            respdata["success"] = "FALSE";
            respdata["reason"] = "region login currently disabled";
            respdata["str_response_string"] = "region login currently disabled";

            return respdata;

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

        /// <summary>
        /// Converts the inventory library skeleton into the form required by the rpc request.
        /// </summary>
        /// <returns></returns>
        protected virtual ArrayList GetInventoryLibrary()
        {
            Dictionary<UUID, InventoryFolderImpl> rootFolders
                = m_libraryRootFolder.RequestSelfAndDescendentFolders();
            ArrayList folderHashes = new ArrayList();

            foreach (InventoryFolderBase folder in rootFolders.Values)
            {
                Hashtable TempHash = new Hashtable();
                TempHash["name"] = folder.Name;
                TempHash["parent_id"] = folder.ParentID.ToString();
                TempHash["version"] = (Int32)folder.Version;
                TempHash["type_default"] = (Int32)folder.Type;
                TempHash["folder_id"] = folder.ID.ToString();
                folderHashes.Add(TempHash);
            }

            if (m_worldAssets != null)
            {
                foreach (InventoryFolderBase folder in m_worldAssets.WorldFolders)
                {
                    Hashtable temp = new Hashtable();
                    temp["name"] = folder.Name;
                    temp["parent_id"] = folder.ParentID.ToString();
                    temp["version"] = (Int32)folder.Version;
                    temp["type_default"] = (Int32)folder.Type;
                    temp["folder_id"] = folder.ID.ToString();
                    folderHashes.Add(temp);
                }
            }

            return folderHashes;
        }

        #endregion


        #region IRexUDPPort Members

        public int GetPort(ulong regionHandle)
        {
            if (m_region_ports.ContainsKey(regionHandle))
            {
                return m_region_ports[regionHandle];
            }
            else
            {
                m_log.Warn("[IRexUDPPort]: Port not found for region handle " + regionHandle);
                return 0;
            }
        }

        /// <summary>
        /// Inefficient way to get port
        /// </summary>
        /// <param name="endPoint">SL endpoint to fetch</param>
        /// <returns>Rex udp port</returns>
        public int GetPort(System.Net.IPEndPoint endPoint)
        {
            foreach (Scene s in m_scenes)
            {
                if (s.RegionInfo.ExternalEndPoint.Address.Equals(endPoint.Address) && s.RegionInfo.ExternalEndPoint.Port == endPoint.Port)
                {
                    return GetPort(s.RegionInfo.RegionHandle);
                }
            }
            m_log.WarnFormat("[IRexUDPPort]: Port not found for IP end point {0}", endPoint);
            return 0;
        }

        #endregion
    }
}
