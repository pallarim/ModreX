using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Services.LLLoginService;
using Nini.Config;
using OpenSim.Services.Interfaces;
using System.Net;
using ModularRex.RexDBObjects;
using OpenMetaverse;
using log4net;
using System.Reflection;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;
using OpenSim.Framework;

namespace ModularRex.RexNetwork.RexLogin
{
    public interface IRexLoginService : ILoginService
    {
        LoginResponse Login(string account, string sessionHash, string startLocation, UUID scopeID, string clientVersion, IPEndPoint clientIP);
    }

    public class RexLoginService : LLLoginService, IRexLoginService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private bool m_UseOSInventory = true;

        public RexLoginService(IConfigSource config, ISimulationService simService, ILibraryService libraryService)
            : base(config, simService, libraryService)
        {
            //TODO: Read configuration about m_UseOSInventory
            //TODO: Read configuration about rexavatar
            //TODO: Load rexavatar plugin
        }

    //    public RexLoginService(IConfigSource config) : this(config, null, null)
    //    {
    //    }

        public LoginResponse Login(string account, string sessionHash, string startLocation, UUID scopeID, string clientVersion, IPEndPoint clientIP)
        {
            bool success = false;
            UUID session = UUID.Random();
            try
            {
                //
                // Authenticate user
                //
                if (!AuthenticateUser(account, sessionHash))
                {
                    return RexFailedLoginResponse.SessionProblem;
                }
                string actName = account.Split('@')[0];
                string actSrv = account.Split('@')[1];

                RexUserProfileData rap = AuthenticationService.GetUserByAccount(actName, actSrv);

                string firstName = rap.FirstName + " " + rap.SurName;
                string lastName = "<" + account + ">";

                //
                // Check if user has logged in before
                //
                UserAccount useraccount = m_UserAccountService.GetUserAccount(UUID.Zero, firstName, lastName);
                if (useraccount != null)
                {
                    //now check if user has required level to login
                    if (useraccount.UserLevel < m_MinLoginLevel)
                    {
                        m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: login is blocked for user level {0}", useraccount.UserLevel);
                        return LLFailedLoginResponse.LoginBlockedProblem;
                    }
                }
                else
                {
                    //TODO: check if we accept this user or new users at all to login
                    if (true)
                    {
                        useraccount = CreateAndSaveUserAccount(firstName, lastName, String.Empty);
                        if (useraccount == null)
                        {
                            return RexFailedLoginResponse.AccountCreationProblem;
                        }
                    }
                    else
                    {
                        return RexFailedLoginResponse.DeniedAccount;
                    }
                }

                //
                // Create random password each time, so no-one can use this account to login with opensim/sl auth
                //
                string passwd = UUID.Random().ToString();
                if (!m_AuthenticationService.SetPassword(useraccount.PrincipalID, passwd))
                {
                    return LLFailedLoginResponse.InternalError;
                }

                //
                // Get secureSession id
                //
                string token = m_AuthenticationService.Authenticate(useraccount.PrincipalID, Util.Md5Hash(passwd), 30);
                UUID secureSession = UUID.Zero;
                if ((token == string.Empty) || (token != string.Empty && !UUID.TryParse(token, out secureSession)))
                {
                    //this shouldn't happend since user doesn't give password. we just fetch secureSession id
                    //m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: authentication failed");
                    return LLFailedLoginResponse.UserProblem;
                }

                //
                // Get the user's inventory
                //
                if (m_UseOSInventory && m_InventoryService == null)
                {
                    m_log.WarnFormat("[REXLOGIN SERVICE]: Login failed, reason: inventory service not set up");
                    return LLFailedLoginResponse.InventoryProblem;
                }
                List<InventoryFolderBase> inventorySkel = m_InventoryService.GetInventorySkeleton(useraccount.PrincipalID); //TODO? Rex magic to inventory skeleton
                if (m_UseOSInventory && ((inventorySkel == null) || (inventorySkel != null && inventorySkel.Count == 0)))
                {
                    m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: unable to retrieve user inventory");
                    return LLFailedLoginResponse.InventoryProblem;
                }

                // Get active gestures
                List<InventoryItemBase> gestures = m_InventoryService.GetActiveGestures(useraccount.PrincipalID);
                m_log.DebugFormat("[LLOGIN SERVICE]: {0} active gestures", gestures.Count);

                //
                // Login the presence
                //
                if (m_PresenceService != null)
                {
                    success = m_PresenceService.LoginAgent(useraccount.PrincipalID.ToString(), session, secureSession);
                    if (!success)
                    {
                        m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: could not login presence");
                        return LLFailedLoginResponse.GridProblem;
                    }
                }

                //
                // Change Online status and get the home region
                //
                GridRegion home = null;
                GridUserInfo guinfo = m_GridUserService.LoggedIn(useraccount.PrincipalID.ToString());
                if (guinfo != null && (guinfo.HomeRegionID != UUID.Zero) && m_GridService != null)
                {
                    home = m_GridService.GetRegionByUUID(scopeID, guinfo.HomeRegionID);
                }
                if (guinfo == null)
                {
                    // something went wrong, make something up, so that we don't have to test this anywhere else
                    guinfo = new GridUserInfo();
                    guinfo.LastPosition = guinfo.HomePosition = new Vector3(128, 128, 30);
                }

                //
                // Find the destination region/grid
                //
                string where = string.Empty;
                Vector3 position = Vector3.Zero;
                Vector3 lookAt = Vector3.Zero;
                GridRegion gatekeeper = null;
                GridRegion destination = FindDestination(useraccount, scopeID, guinfo, session, startLocation, home, out gatekeeper, out where, out position, out lookAt);
                if (destination == null)
                {
                    m_PresenceService.LogoutAgent(session);
                    m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: destination not found");
                    return LLFailedLoginResponse.GridProblem;
                }

                //
                // Get the os avatar
                //
                AvatarData avatar = null;
                if (m_AvatarService != null)
                {
                    avatar = m_AvatarService.GetAvatar(useraccount.PrincipalID);
                }

                //
                // Instantiate/get the simulation interface and launch an agent at the destination
                //
                string reason = string.Empty;
                GridRegion dest;
                AgentCircuitData aCircuit = LaunchAgentAtGrid(gatekeeper, destination, useraccount, avatar, session, secureSession, position, where, clientVersion, clientIP, out where, out reason, out dest);

                if (aCircuit == null)
                {
                    m_PresenceService.LogoutAgent(session);
                    m_log.InfoFormat("[REXLOGIN SERVICE]: Login failed, reason: {0}", reason);
                    return new LLFailedLoginResponse("key", reason, "false");

                }
                // Get Friends list 
                FriendInfo[] friendsList = new FriendInfo[0];
                if (m_FriendsService != null)
                {
                    friendsList = m_FriendsService.GetFriends(useraccount.PrincipalID);
                    m_log.DebugFormat("[REXLOGIN SERVICE]: Retrieved {0} friends", friendsList.Length);
                }

                //
                // Finally, fill out the response and return it
                //
                RexLoginResponse response = new RexLoginResponse(useraccount, aCircuit, guinfo, destination, inventorySkel, friendsList, m_LibraryService,
                    where, startLocation, position, lookAt, gestures, m_WelcomeMessage, home, clientIP, m_MapTileURL, m_SearchURL);

                m_log.DebugFormat("[REXLOGIN SERVICE]: All clear. Sending login response to client.");
                return response;

            }
            catch (Exception e)
            {
                m_log.WarnFormat("[REXLOGIN SERVICE]: Exception processing login for {0} : {1} {2}", account, e.ToString(), e.StackTrace);
                if (m_PresenceService != null)
                    m_PresenceService.LogoutAgent(session);
                return LLFailedLoginResponse.InternalError;
            }
        }

        private UserAccount CreateAndSaveUserAccount(string firstName, string lastName, string email)
        {
            UserAccount account = new UserAccount(UUID.Zero, firstName, lastName, email);

            if (account.ServiceURLs == null || (account.ServiceURLs != null && account.ServiceURLs.Count == 0))
            {
                account.ServiceURLs = new Dictionary<string, object>();
                account.ServiceURLs["HomeURI"] = string.Empty;
                account.ServiceURLs["GatekeeperURI"] = string.Empty;
                account.ServiceURLs["InventoryServerURI"] = string.Empty;
                account.ServiceURLs["AssetServerURI"] = string.Empty;
            }

            if (m_UserAccountService.StoreUserAccount(account))
            {
                //skip password storing, because we don't use it here

                //GridRegion home = null;
                //if (m_GridService != null)
                //{
                //    List<GridRegion> defaultRegions = m_GridService.GetDefaultRegions(UUID.Zero);
                //    if (defaultRegions != null && defaultRegions.Count >= 1)
                //        home = defaultRegions[0];

                //    if (m_PresenceService != null && home != null)
                //        m_PresenceService.SetHomeLocation(account.PrincipalID.ToString(), home.RegionID, new Vector3(128, 128, 0), new Vector3(0, 1, 0));
                //    else
                //        m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to set home for account {0} {1}.",
                //           firstName, lastName);

                //}
                //else
                //    m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to retrieve home region for account {0} {1}.",
                //       firstName, lastName);

                bool success = false;
                //TODO: check if we want to create inventories for new users
                if (m_InventoryService != null)
                    success = m_InventoryService.CreateUserInventory(account.PrincipalID);
                if (!success)
                    m_log.WarnFormat("[USER ACCOUNT SERVICE]: Unable to create inventory for account {0} {1}.",
                       firstName, lastName);

                return account;
            }
            return null;
        }

        private bool AuthenticateUser(string account, string sessionHash)
        {
            string actName = account.Split('@')[0];
            string actSrv = account.Split('@')[1];
            return AuthenticationService.SimAuthenticationAccount(actName, sessionHash, actSrv);
        }
    }
}
