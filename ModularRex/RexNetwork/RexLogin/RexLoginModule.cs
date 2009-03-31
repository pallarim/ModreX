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
using OpenSim.Framework.Communications.Services;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Communications.Cache;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexLoginModule : IRegionModule 
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly List<Scene> m_scenes = new List<Scene>();
        private readonly Dictionary<UUID, string> m_authUrl = new Dictionary<UUID, string>();

        private List<RexUDPServer> m_udpservers = new List<RexUDPServer>();
        private IConfigSource m_config;

        private RegionInfo m_primaryRegionInfo;
        private uint m_rexPort = 7000; //TODO: Make this obselete, use dictionary instead of single default value

        private OpenSim.Framework.Servers.XmlRpcMethod default_login_to_simulator;

        /// <summary>
        /// Used during login to send the skeleton of the OpenSim Library to the client.
        /// </summary>
        protected LibraryRootFolder m_libraryRootFolder;

        public void Initialise(Scene scene, IConfigSource source)
        {
            //if (m_udpserver == null)
            //    m_udpserver = new RexUDPServer();

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
                if (m_authUrl.ContainsKey(rex.AgentId))
                {
                    rex.RexAccount = m_authUrl[rex.AgentId];
                }
                else
                {
                    m_log.WarnFormat("[REX] Client {0} does not have realXtend acccount", rex.AgentId);
                }
            }
        }


        public void PostInitialise()
        {
            if (m_config.Configs["realXtend"].GetBoolean("enabled", true))
            {
                //Load OpenSim Library folder config
                string LibrariesXMLFile = m_config.Configs["StandAlone"].GetString("LibrariesXMLFile");
                m_libraryRootFolder = new LibraryRootFolder(LibrariesXMLFile);

                m_log.Info("[REX] Overloading Login_to_Simulator");
                default_login_to_simulator = m_scenes[0].CommsManager.HttpServer.GetXmlRPCHandler("login_to_simulator");
                m_scenes[0].CommsManager.HttpServer.AddXmlRPCHandler("login_to_simulator", XmlRpcLoginMethod);

                m_primaryRegionInfo = m_scenes[0].RegionInfo;

                m_log.Info("[REX] Initialising");
                //m_udpserver.Initialise(m_primaryRegionInfo.InternalEndPoint.Address, ref m_rexPort, 0, false, m_config, m_scenes[0].AssetCache,
                //                       m_scenes[0].AuthenticateHandler);
                foreach (Scene scene in m_scenes)
                {
                    RexUDPServer udpserver = new RexUDPServer();
                    m_log.Debug("[REX] RegionInfo: " + scene.RegionInfo.InternalEndPoint.Port);
                    uint udp_port = Convert.ToUInt32(scene.RegionInfo.InternalEndPoint.Port - 2000);
                    //uses 2000 smaller port num than spesified for LLClient in Regions/ xml-file
                    udpserver.Initialise(scene.RegionInfo.InternalEndPoint.Address, ref udp_port, 0, false, m_config,
                        scene.CommsManager.AssetCache, scene.AuthenticateHandler);
                    udpserver.AddScene(scene);
                    //m_udpserver.AddScene(scene); //this doesn't add scene to existing UDP server, but instead replaces the old one
                    m_udpservers.Add(udpserver);
                    udpserver.Start();
                }
                //m_udpserver.Start();
            }
            else
            {
                m_log.Info("[REX] Not overloading Login_to_Simulator and not starting UDP server");
            }
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

        // TEMP // TEMP //

        public class RexAccountProperties
        {
            public string FirstName;
            public string LastName;
            public string AsAddress;
            public UUID uuid;
            public string Account;
        }

        public RexAccountProperties GetRexProperties(string RexAccount, string RexAuthURL)
        {
            m_log.Info("[REXCLIENT] Resolving avatar...");
            Hashtable ReqVals = new Hashtable();
            ReqVals["avatar_account"] = RexAccount;
            ReqVals["AuthenticationAddress"] = RexAuthURL;

            ArrayList SendParams = new ArrayList();
            SendParams.Add(ReqVals);

            XmlRpcRequest req = new XmlRpcRequest("get_user_by_account", SendParams);

            m_log.Info("[REXCLIENT] Sending XMLRPC Request to http://" + RexAuthURL);

            XmlRpcResponse authreply = req.Send("http://" + RexAuthURL, 9000);

            //m_log.Info(authreply.ToString());

            string rexAsAddress = ((Hashtable) authreply.Value)["as_address"].ToString();
            /*            string rexSkypeURL = ((Hashtable)authreply.Value)["skype_url"].ToString(); */
            UUID userID = new UUID(((Hashtable) authreply.Value)["uuid"].ToString());

            RexAccountProperties rtn = new RexAccountProperties();

            rtn.FirstName = ((Hashtable)authreply.Value)["firstname"].ToString();
            rtn.LastName = ((Hashtable)authreply.Value)["lastname"].ToString();
            rtn.AsAddress = ((Hashtable)authreply.Value)["as_address"].ToString();
            rtn.uuid = userID;
            rtn.Account = RexAccount;

            return rtn;
        }

        // TEMP // TEMP //

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
                    XmlRpcResponse rep = default_login_to_simulator(request);
                    Hashtable val = (Hashtable)rep.Value;
                    val["rex"] = "running rex mode";
                    val["sim_port"] = (Int32)m_primaryRegionInfo.ExternalEndPoint.Port - 2000; //TODO: fix this to get from dictionary
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
                        return default_login_to_simulator(request);
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

                RexAccountProperties rap = GetRexProperties(actName, actSrv);

                UUID agentID = rap.uuid;

                // Used to transmit the login URL to the 
                // RexAvatar class when it connects.
                m_authUrl[agentID] = account;

                logResponse.CircuitCode = Util.RandomClass.Next();

                logResponse.Lastname = "<" + account + ">";
                logResponse.Firstname = rap.FirstName + " " + rap.LastName;

                logResponse.AgentID = agentID;

                // NOT SECURE
                logResponse.SessionID = GetSessionID(account);
                logResponse.SecureSessionID = GetSecureID(account);

                logResponse.Message = "Welcome to ModularRex";

                logResponse.SimAddress = m_primaryRegionInfo.ExternalEndPoint.Address.ToString();
                logResponse.SimPort = (uint)m_primaryRegionInfo.ExternalEndPoint.Port - 2000; //TODO: fix this to get from dictionary
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
                if (m_scenes[0].CommsManager.UserAdminService != null)
                {
                    m_scenes[0].CommsManager.UserAdminService.AddUser(logResponse.Firstname, logResponse.Lastname, "",
                                                                      account, 1000, 1000, agentID);
                }
                if (m_scenes[0].CommsManager.InterServiceInventoryService != null)
                {
                    m_scenes[0].CommsManager.InterServiceInventoryService.CreateNewUserInventory(agentID);
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

                    scene.NewUserConnection(acd);
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

            return folderHashes;
        }

        #endregion

    }
}