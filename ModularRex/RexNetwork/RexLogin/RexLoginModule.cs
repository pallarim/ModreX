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
    public class RexLoginModule : INonSharedRegionModule, IRexUDPPort
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
        private bool firstScene = true;

        private Type cb_ws_connector = null;
        private MethodInfo cb_ws_method = null;
        private bool cb_ws_try_load = true;

        /// <summary>
        /// Used during login to send the skeleton of the OpenSim Library to the client.
        /// </summary>
        protected LibraryRootFolder m_libraryRootFolder;

        protected WorldAssetsFolder m_worldAssets;

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
                    //ok, no userdata found
                    //maybe client logged in with cb_connector
                    //let's try to find that information
                    IRegionModuleBase cb_module = null;
                    try
                    {
                        cb_module = m_scenes[0].RegionModules["CableBeachWorldServiceConnector"];
                    }
                    catch (Exception)
                    {
                    }

                    if (cb_module != null)
                    {
                        //yes, user logged with cb connector
                        //let's check if we have already reference for the assembly
                        if (cb_ws_connector == null && cb_ws_try_load)
                        {
                            //we haven't loaded the assembly, let's do that now
                            try
                            {
                                cb_ws_connector = System.Reflection.Assembly.Load("ModCableBeach.dll").GetType("ModCableBeach.WorldServiceConnector");
                                Type[] parameterTypes = new Type[3];
                                parameterTypes[0] = typeof(UUID);
                                parameterTypes[1] = typeof(Uri);
                                parameterTypes[2] = typeof(Uri);
                                cb_ws_method = cb_ws_connector.GetMethod("GetServiceCapability", parameterTypes);
                            }
                            catch (Exception e)
                            {
                                //could not load assembly. don't try to load it anymore until restart
                                m_log.WarnFormat("[REX] Client {0} does not have realXtend acccount", rex.AgentId);
                                cb_ws_try_load = false;
                                return;
                            }
                        }

                        //try with legacy parameters
                        object[] parameters = new object[3];
                        parameters[0] = rex.AgentId;
                        parameters[1] = new Uri("http://www.realxtend.org/attributes/avatarStorageUri");
                        parameters[2] = new Uri("http://www.realxtend.org/attributes/avatarStorageUri");
                        object returnObject = cb_ws_method.Invoke(cb_module, parameters);

                        if (returnObject != null)
                        {
                            if (returnObject is Uri)
                            {
                                rex.RexAvatarURL = ((Uri)returnObject).ToString();
                            }
                        }
                        else
                        {
                            //now try with new style (webdav based avatar)
                            parameters[1] = new Uri("http://openmetaverse.org/services/webdav_avatar");
                            parameters[2] = new Uri("http://openmetaverse.org/services/webdav_avatar/get_webdav_root");
                            returnObject = cb_ws_method.Invoke(cb_module, parameters);

                            if (returnObject is Uri)
                            {
                                rex.RexAvatarURL = ((Uri)returnObject).ToString();
                            }
                        }
                    }
                    else
                    {
                        m_log.WarnFormat("[REX] Client {0} does not have realXtend acccount", rex.AgentId);
                    }
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
            get { return "RexLoginOverrider"; }
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
            List<InventoryFolderBase> folders = null; 
            if (any.InventoryService != null)
            {
                // local service (standalone)
                folders = any.InventoryService.GetInventorySkeleton(userID);

                // If we have user auth but no inventory folders for some reason, create a new set of folders.
                if (folders == null || 0 == folders.Count)
                {
                    any.InventoryService.CreateUserInventory(userID);
                    folders = any.InventoryService.GetInventorySkeleton(userID);
                }
            }
            else if (any.CommsManager.InterServiceInventoryService != null)
            {
                // used by the user server
                folders = any.CommsManager.InterServiceInventoryService.GetInventorySkeleton(userID);

                // If we have user auth but no inventory folders for some reason, create a new set of folders.
                if (null == folders || 0 == folders.Count)
                {
                    any.CommsManager.InterServiceInventoryService.CreateNewUserInventory(userID);
                    folders = any.CommsManager.InterServiceInventoryService.GetInventorySkeleton(userID);
                }
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

                logResponse.Message = m_config.Configs["StandAlone"].GetString("welcome_message", "Welcome to ModularRex");

                logResponse.SimAddress = m_primaryRegionInfo.ExternalEndPoint.Address.ToString();
                logResponse.SimPort = (uint)GetPort(m_primaryRegionInfo.RegionHandle);
                logResponse.RegionX = m_primaryRegionInfo.RegionLocX;
                logResponse.RegionY = m_primaryRegionInfo.RegionLocY;


                logResponse.StartLocation = startLocationRequest;

                string capsPath = OpenSim.Framework.Capabilities.CapsUtil.GetRandomCapsObjectPath();
                string httpServerURI = "http://" + m_primaryRegionInfo.ExternalHostName + ":" + m_primaryRegionInfo.HttpPort;
                //string seedcap = "http://" + m_scenes[0].RegionInfo.ExternalEndPoint.Address + ":" +
                //                 "9000" + "/CAPS/" + capsPath + "0000/";
                string seedcap = httpServerURI + OpenSim.Framework.Capabilities.CapsUtil.GetCapsSeedPath(capsPath);//capsPath;// + "0000/";

                logResponse.SeedCapability = seedcap;

                bool newUser = false;
                UserProfileData user = m_scenes[0].CommsManager.UserService.GetUserProfile(agentID);
                if (user == null)
                {
                    m_scenes[0].CommsManager.UserAdminService.AddUser(logResponse.Firstname, logResponse.Lastname, "",
                                                                      account, 1000, 1000, agentID);
                    user = m_scenes[0].CommsManager.UserService.GetUserProfile(agentID);
                    newUser = true;
                }

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

                if (newUser)
                {
                    if (m_scenes[0].InventoryService != null)
                    {
                        // local service (standalone)
                        m_scenes[0].InventoryService.CreateUserInventory(agentID);

                        //m_log.Debug("[USERSTORAGE]: using IInventoryService to create user's inventory");
                        //m_InventoryService.CreateUserInventory(userProf.ID);
                        //InventoryFolderBase rootfolder = m_InventoryService.GetRootFolder(userProf.ID);
                        //if (rootfolder != null)
                        //    userProf.RootInventoryFolderID = rootfolder.ID;
                    }
                    else if (m_scenes[0].CommsManager.InterServiceInventoryService != null)
                    {
                        // used by the user server
                        m_scenes[0].CommsManager.InterServiceInventoryService.CreateNewUserInventory(agentID);

                        //m_log.Debug("[USERSTORAGE]: using m_commsManager.InterServiceInventoryService to create user's inventory");
                        //m_commsManager.InterServiceInventoryService.CreateNewUserInventory(userProf.ID);
                    }
                }

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

                Hashtable val = (Hashtable)rep.Value;
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

        #region IRegionModuleBase Members

        public void AddRegion(Scene scene)
        {
            m_scenes.Add(scene);

            scene.EventManager.OnClientConnect += EventManager_OnClientConnect;
            scene.RegisterModuleInterface<IRexUDPPort>(this);
            if (m_config.Configs["realXtend"] != null && m_config.Configs["realXtend"].GetBoolean("enabled", false))
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

                uint _udpport = (uint)m_nextUdpPort;
                RexUDPServer udpserver = new RexUDPServer(scene.RegionInfo.InternalEndPoint.Address, ref _udpport, 0, false, m_config, scene.AuthenticateHandler);
                //udpserver.Initialise(scene.RegionInfo.InternalEndPoint.Address, ref _udpport, 0, false, m_config, scene.AuthenticateHandler);
                udpserver.AddScene(scene);

                m_region_ports.Add(scene.RegionInfo.RegionHandle, m_nextUdpPort);
                m_udpservers.Add(udpserver);
                udpserver.Start();
                m_nextUdpPort++;
            }
        }

        public void Initialise(IConfigSource source)
        {
            m_config = source;

            if (m_config.Configs["realXtend"] != null && m_config.Configs["realXtend"].GetBoolean("enabled", false))
            {
                m_log.Info("[REX] Initialising");
            }
            else
            {
                m_log.Info("[REX] Not overloading Login_to_Simulator and not starting UDP server");
            }
        }

        public void RegionLoaded(Scene scene)
        {
            if (m_config.Configs["realXtend"] != null && m_config.Configs["realXtend"].GetBoolean("enabled", false))
            {
                if (firstScene)
                {
                    firstScene = false;

                    //Do this here because LLStandaloneLoginModule will register it's own login method in Initializion for each Scene
                    m_log.Info("[REX] Overloading Login_to_Simulator");
                    default_login_to_simulator = MainServer.Instance.GetXmlRPCHandler("login_to_simulator");
                    MainServer.Instance.AddXmlRPCHandler("login_to_simulator", XmlRpcLoginMethod);

                    m_worldAssets = m_scenes[0].RequestModuleInterface<WorldAssetsFolder>();
                    if (m_worldAssets != null)
                        m_libraryRootFolder.AddChildFolder(m_worldAssets);
                }  
            }
        }

        public void RemoveRegion(Scene scene)
        {
            m_scenes.Remove(scene);
            scene.EventManager.OnClientConnect -= EventManager_OnClientConnect;
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        #endregion
    }
}
