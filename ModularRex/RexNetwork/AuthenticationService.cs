using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using OpenSim.Framework;
using Nwc.XmlRpc;
using log4net;
using ModularRex.RexDBObjects;
using OpenMetaverse;

namespace ModularRex.RexNetwork
{
    public static class AuthenticationService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #region Static methods

        public static RexUserProfileData GetUserByAccount(string account, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: GetUserByAccount");

            try
            {

                if (account == null || account.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByAccount: Parameters invalid.");
                    return null;
                }

                if (account.Contains("@"))
                {
                    account = account.Split('@')[0];
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("account", account);

                XmlRpcResponse response = DoRequest("get_user_by_account", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByAccount: Response is null.");
                    return null;
                }

                RexUserProfileData userProfile = ConvertXMLRPCDataToUserProfile((Hashtable)response.Value);

                if (userProfile == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByAccount: UserProfile is null.");
                    return null;
                }

                return userProfile;

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetUserByAccount", e);
            }

            return null;

        }

        public static RexUserProfileData GetUserByName(string avatarName, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: GetUserByName");

            try
            {

                if (avatarName == null || avatarName.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByName: Parameters invalid.");
                    return null;
                }


                Hashtable requestData = new Hashtable();
                requestData.Add("avatar_name", avatarName);

                XmlRpcResponse response = DoRequest("get_user_by_name", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByName: Response is null.");
                    return null;
                }

                RexUserProfileData userProfile = ConvertXMLRPCDataToUserProfile((Hashtable)response.Value);

                if (userProfile == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByName: UserProfile is null.");
                    return null;
                }

                return userProfile;

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetUserByName", e);
            }

            return null;

        }

        public static UserProfileData GetUserByUuid(string avatarUuid, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: GetUserByUuid");

            try
            {

                if (avatarUuid == null || avatarUuid.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByUuid: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("avatar_uuid", avatarUuid);

                XmlRpcResponse response = DoRequest("get_user_by_uuid", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByUuid: Response is null.");
                    return null;
                }

                RexUserProfileData userProfile = ConvertXMLRPCDataToUserProfile((Hashtable)response.Value);

                if (userProfile == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserByUuid: UserProfile is null.");
                    return null;
                }

                return userProfile;

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetUserByUuid", e);
            }

            return null;
           
        }

        public static List<AvatarPickerAvatar> GetAvatarPickerAvatar(string avquery, string queryid, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: GetAvatarPickerAvatar");

            try
            {

                if (avquery == null || avquery.Length == 0 || 
                    queryid == null || queryid.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetAvatarPickerAvatar: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("avquery", avquery);
                requestData.Add("queryid", queryid);

                XmlRpcResponse response = DoRequest("get_avatar_picker_avatar", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetAvatarPickerAvatar: Response is null.");
                    return null;
                }

                List<AvatarPickerAvatar> pickerList = ConvertXMLRPCDataToAvatarPickerList(new UUID(queryid), (Hashtable)response.Value);

                return pickerList;

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetAvatarPickerAvatar", e);
            }

            return null;

        }


        public static bool StorageAuthentication(string account, string sessionHash, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: StorageAuthentication");

            try
            {

                if (account == null || account.Length == 0 || 
                    sessionHash == null || sessionHash.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] StorageAuthentication: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("account", account);
                requestData.Add("sessionhash", sessionHash);

                XmlRpcResponse response = DoRequest("StorageAuthentication", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] StorageAuthentication: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("login"))
                {
                    string login = (string)responseData["login"];
                    if (login.Equals("success"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: StorageAuthentication", e);
            }

            return false;

        }

        public static bool RemoveUserAgent(string agentID, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: RemoveUserAgent");

            try
            {
                if (agentID == null || agentID.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] RemoveUserAgent: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("agentID", agentID);

                XmlRpcResponse response = DoRequest("remove_user_agent", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] RemoveUserAgent: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("remove"))
                {
                    string login = (string)responseData["remove"];
                    if (login.Equals("success"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: RemoveUserAgent", e);
            }

            return false;

        }

        public static string SimAuthenticationAccount(string account, string sessionHash, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: SimAuthenticationAccount with account");

            try
            {

                if (account == null || account.Length == 0 || 
                    sessionHash == null || sessionHash.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SimAuthenticationAccount: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("account", account);
                requestData.Add("sessionhash", sessionHash);

                XmlRpcResponse response = DoRequest("SimAuthenticationAccount", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SimAuthenticationAccount: Response is null.");
                    return null;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("login") && responseData.Contains("sessionHash"))
                {
                    string login = (string)responseData["login"];
                    string sessionHashTemp = (string)responseData["sessionHash"];
                    if (login.Equals("success") && sessionHashTemp != null)
                    {
                        return sessionHashTemp;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: SimAuthenticationAccount", e);
            }

            return null;
            
        }


        public static bool ClientAuthentication(string account, string passwd, string authUrl, out string sessionHash, out string gridUrl, out string avatarStorageUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: ClientAuthentication");

            gridUrl = "";
            avatarStorageUrl = "";
            sessionHash = "";

            try
            {

                if (account == null || account.Length == 0 || passwd == null || passwd.Length == 0 || authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] ClientAuthentication: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("account", account);
                requestData.Add("passwd", passwd);

                XmlRpcResponse response = DoRequest("ClientAuthentication", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] ClientAuthentication: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("sessionHash") && responseData.Contains("avatarStorageUrl") && requestData.Contains("gridUrl"))
                {
                    sessionHash = (string)responseData["sessionHash"];
                    string gridUrlTemp = (string)responseData["gridUrl"];
                    if (gridUrlTemp != null)
                    {
                        gridUrl = gridUrlTemp;
                    }
                    avatarStorageUrl = (string)responseData["avatarStorageUrl"];

                    return true;
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: ClientAuthentication", e);
            }

            return false;
        }

        public static bool SetSessionAuthentication(string account, string passwd, string authUrl, out string sessionHash, out string gridUrl, out string avatarStorageUrl)
        
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: SetSessionAuthentication");

            gridUrl = "";
            avatarStorageUrl = "";
            sessionHash = "";

            try
            {

                if (account == null || account.Length == 0 || passwd == null || passwd.Length == 0 || authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SetSessionAuthentication: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("account", account);
                requestData.Add("passwd", passwd);

                XmlRpcResponse response = DoRequest("SetSessionAuthentication", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SetSessionAuthentication: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("sessionHash") && responseData.Contains("avatarStorageUrl") && requestData.Contains("gridUrl"))
                {
                    sessionHash = (string)responseData["sessionHash"];
                    string gridUrlTemp = (string)responseData["gridUrl"];
                    if (gridUrlTemp != null)
                    {
                        gridUrl = gridUrlTemp;
                    }
                    avatarStorageUrl = (string)responseData["avatarStorageUrl"];

                    return true;
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: SetSessionAuthentication", e);
            }

            return false;

        }

        public static bool UpdateUserProfile (string ID, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: UpdateUserProfile");

            try
            {

                if (ID == null || ID.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] UpdateUserProfile: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ID", ID);

                XmlRpcResponse response = DoRequest("update_user_profile", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] UpdateUserProfile: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("request"))
                {
                    string request = (string)responseData["request"];
                    if (request.Equals("success"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: UpdateUserProfile", e);
            }

            return false;

        }

        public static bool UpdateUserAgent(string agentID, string agentOnline, string logoutTime, string agentCurrentPosX, string agentCurrentPosY, string agentCurrentPosZ, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: UpdateUserAgent");

            try
            {

                if (agentID == null || agentID.Length == 0 ||
                    agentOnline == null || agentOnline.Length == 0 ||
                    logoutTime == null || logoutTime.Length == 0 ||
                    agentCurrentPosX == null || agentCurrentPosX.Length == 0 ||
                    agentCurrentPosY == null || agentCurrentPosY.Length == 0 ||
                    agentCurrentPosZ == null || agentCurrentPosZ.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] UpdateUserAgent: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("agentID", agentID);
                requestData.Add("agentOnline", agentOnline);
                requestData.Add("logoutTime", logoutTime);
                requestData.Add("agent_CurrentPosX", agentCurrentPosX);
                requestData.Add("agent_CurrentPosY", agentCurrentPosY);
                requestData.Add("agent_CurrentPosZ", agentCurrentPosZ);

                XmlRpcResponse response = DoRequest("update_user_agent", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] UpdateUserAgent: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("update"))
                {
                    string request = (string)responseData["update"];
                    if (request.Equals("success"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: UpdateUserAgent", e);
            }

            return false;            
        }

        public static bool AddUserFriend(string ownerID, string friendID, string friendPerms, string friendAuth, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: AddUserFriend");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    friendPerms == null || friendPerms.Length == 0 ||
                    friendAuth == null || friendAuth.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] AddUserFriend: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);
                requestData.Add("friendPerms", friendPerms);
                requestData.Add("friendAuth", friendAuth);

                XmlRpcResponse response = DoRequest("add_new_user_friend", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] AddUserFriend: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("friend"))
                {                
                    return true;   
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: AddUserFriend", e);
            }

            return false;
        }

        public static bool RemoveUserFriend(string ownerID, string friendID, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: RemoveUserFriend");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] RemoveUserFriend: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);

                XmlRpcResponse response = DoRequest("remove_user_friend", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] RemoveUserFriend: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("friend"))
                {
                    return true;
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: RemoveUserFriend", e);
            }

            return false;

        }

        public static List<RexFriendListItem> GetUserFriendList(string ownerID, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: GetUserFriendList");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserFriendList: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);

                XmlRpcResponse response = DoRequest("get_user_friend_list", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetUserFriendList: Response is null.");
                    return null;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("avcount"))
                {

                    int count = Convert.ToInt32(responseData["avcount"]);
                    if (count > 0)
                    {
                        List<RexFriendListItem> friends = new List<RexFriendListItem>();
                        for (int i = 0; i < count; i++)
                        {
                            RexFriendListItem friend = new RexFriendListItem();
                            friend.Friend = new UUID(responseData["friendID"+i].ToString());
                            friend.FriendListOwner = new UUID(responseData["ownerID"+i].ToString());
                            friend.FriendListOwnerPerms = Convert.ToUInt32(responseData["ownerPerms" + i].ToString());
                            friend.FriendPerms = Convert.ToUInt32(responseData["friendPerms"+i].ToString());
                            friend.AuthAddress = responseData["authAddr"+i].ToString();
                            friend.onlinestatus = Convert.ToBoolean(responseData["online"+i].ToString());
                            
                        }
                        return friends;

                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetUserFriendList", e);
            }

            return null;

        }

        public static Hashtable GetOnlineStatusList(string ownerID, List<string> uuids, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: GetOnlineStatusList");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    authUrl == null || authUrl.Length == 0 ||
                    uuids == null || uuids.Count == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetOnlineStatusList: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                foreach (var struid in uuids)
                {
                    requestData.Add(struid, struid);
                }

                XmlRpcResponse response = DoRequest("get_user_friend_list", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetOnlineStatusList: Response is null.");
                    return null;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetOnlineStatusList: ResponseData is null.");
                    return null;
                }

                return responseData;

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetOnlineStatusList", e);
            }

            return null;

        }

        public static void FriendOfflineNotification(string ownerID, string friendID, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: FriendOfflineNotification");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] FriendOfflineNotification: Parameters invalid.");
                    return;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);

                DoRequest("friend_offline_notification", requestData, authUrl);

                m_log.Info("[AUTHENTICATIONSERVICE] FriendOfflineNotification: request sent.");

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: FriendOfflineNotification", e);
            }

        }

        public static void FriendOfflineNotification(string ownerID, string friendID, string currentHandle, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: FriendOfflineNotification");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    currentHandle == null || currentHandle.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] FriendOfflineNotification: Parameters invalid.");
                    return;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);
                requestData.Add("currentHandle", currentHandle);

                DoRequest("friend_offline_notification", requestData, authUrl);

                m_log.Info("[AUTHENTICATIONSERVICE] FriendOfflineNotification: request sent.");

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: FriendOfflineNotification", e);
            }

        }

        public static void FriendOnlineNotification(string ownerID, string friendID, string currentHandle, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: FriendOnlineNotification");

            try
            {
                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    currentHandle == null || currentHandle.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] FriendOnlineNotification: Parameters invalid.");
                    return;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);
                requestData.Add("currentHandle", currentHandle);

                DoRequest("friend_online_notification", requestData, authUrl);

                m_log.Info("[AUTHENTICATIONSERVICE] FriendOnlineNotification: request sent.");
            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: FriendOnlineNotification", e);
            }
            
        }

        public static void FriendOnlineNotification(string ownerID, string count, string currentHandle, List<string> friendIDs, string authUrl)
        {

            m_log.Info("[AUTHENTICATIONSERVICE]: FriendOnlineNotification");

            try
            {
                if (ownerID == null || ownerID.Length == 0 ||
                    count == null || count.Length == 0 ||
                    friendIDs == null || friendIDs.Count == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] FriendOnlineNotification: Parameters invalid.");
                    return;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("count", count);
                for (int i = 0; i < friendIDs.Count; i++)
                {
                    requestData.Add("friendID" + i, friendIDs[i].ToString());
                }
                requestData.Add("currentHandle", currentHandle);

                DoRequest("friend_online_notification", requestData, authUrl);

                m_log.Info("[AUTHENTICATIONSERVICE] FriendOnlineNotification: request sent.");
            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: FriendOnlineNotification", e);
            }

        }


        public static string GetFriendLoginAddress(string ownerID, string friendID, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: GetFriendLoginAddress with account");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 || 
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetFriendLoginAddress: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);

                XmlRpcResponse response = DoRequest("get_friend_login_address", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetFriendLoginAddress: Response is null.");
                    return null;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("login") && responseData.Contains("url"))
                {
                    string login = (string)responseData["login"];
                    string url = (string)responseData["url"];
                    if (login.Equals("success") && url != null)
                    {
                        return url;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetFriendLoginAddress", e);
            }

            return null;

        }

        public static string GetFriendAuthenticationAddress(string ownerID, string friendID, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: GetFriendAuthenticationAddress with account");

            try
            {

                if (ownerID == null || ownerID.Length == 0 ||
                    friendID == null || friendID.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetFriendAuthenticationAddress: Parameters invalid.");
                    return null;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("ownerID", ownerID);
                requestData.Add("friendID", friendID);

                XmlRpcResponse response = DoRequest("get_friend_auth_address", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] GetFriendAuthenticationAddress: Response is null.");
                    return null;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("login") && responseData.Contains("url"))
                {
                    string login = (string)responseData["login"];
                    string url = (string)responseData["url"];
                    if (login.Equals("success") && url != null)
                    {
                        return url;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: GetFriendAuthenticationAddress", e);
            }

            return null;

        }
        public static bool InventoryCreationAuthentication(string userID, string sessionHash, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: InventoryCreationAuthentication");

            try
            {

                if (userID == null || userID.Length == 0 ||
                    sessionHash == null || sessionHash.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] InventoryCreationAuthentication: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("userID", userID);
                requestData.Add("sessionhash", sessionHash);

                XmlRpcResponse response = DoRequest("inventory_creation_authentication", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] InventoryCreationAuthentication: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("invent_creation") && responseData.Contains("info"))
                {
                    if (responseData["invent_creation"].Equals("authenticated") && responseData["info"].Equals("success"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: InventoryCreationAuthentication", e);
            }

            return false;
           
        }

        public static bool SendGlobalInstantMessage(string fromAgent, string toAgent, string authUrl)
        {
            m_log.Info("[AUTHENTICATIONSERVICE]: SendGlobalInstantMessage");

            try
            {

                if (fromAgent == null || fromAgent.Length == 0 ||
                    toAgent == null || toAgent.Length == 0 ||
                    authUrl == null || authUrl.Length == 0)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SendGlobalInstantMessage: Parameters invalid.");
                    return false;
                }

                Hashtable requestData = new Hashtable();
                requestData.Add("fromAgent", fromAgent);
                requestData.Add("toAgent", toAgent);

                XmlRpcResponse response = DoRequest("send_global_instant_message", requestData, authUrl);

                if (response == null)
                {
                    m_log.Warn("[AUTHENTICATIONSERVICE] SendGlobalInstantMessage: Response is null.");
                    return false;
                }

                Hashtable responseData = (Hashtable)response.Value;
                if (responseData != null && responseData.Contains("request"))
                {
                    if (responseData["request"].Equals("message sent"))
                    {
                        return true;
                    }
                }

            }
            catch (Exception e)
            {
                m_log.Error("[AUTHENTICATIONSERVICE]: SendGlobalInstantMessage", e);
            }

            return false;

        }

        #endregion

        #region XmlRpc request

        public static XmlRpcResponse DoRequest(string method, Hashtable requestParams, string sendAddr)
        {
            try
            {
                ArrayList SendParams = new ArrayList();
                SendParams.Add(requestParams);
                XmlRpcRequest req = new XmlRpcRequest(method, SendParams);
                if (!sendAddr.StartsWith("http://"))
                    sendAddr = "http://" + sendAddr;
                return req.Send(sendAddr, 3000);
            }
            catch (Exception except)
            {
                m_log.Error("[AUTHENTICATION SERVICE]: Failed creating XmlRpcRequest", except);
                return null;
            }
        }

        #endregion

        #region XmlRpc data to objects

        public static RexUserProfileData ConvertXMLRPCDataToUserProfile(Hashtable data)
        {

            try
            {

            if (data.Contains("error_type"))
            {
                m_log.Warn("[AUTHENTICATIONSERVICE]: " +
                           "Error sent by authentication server server when trying to get user profile: (" +
                           data["error_type"] +
                           "): " + data["error_desc"]);
                return null;
            }

                RexUserProfileData userData = new RexUserProfileData();
                userData.FirstName = (string)data["firstname"];
                userData.SurName = (string)data["lastname"];
                userData.ID = new UUID((string)data["uuid"]);
                userData.UserInventoryURI = (string)data["server_inventory"];
                userData.UserAssetURI = (string)data["server_asset"];
                userData.FirstLifeAboutText = (string)data["profile_firstlife_about"];
                userData.FirstLifeImage = new UUID((string)data["profile_firstlife_image"]);
                userData.CanDoMask = Convert.ToUInt32((string)data["profile_can_do"]);
                userData.WantDoMask = Convert.ToUInt32(data["profile_want_do"]);
                userData.AboutText = (string)data["profile_about"];
                userData.Image = new UUID((string)data["profile_image"]);
                userData.LastLogin = Convert.ToInt32((string)data["profile_lastlogin"]);
                userData.HomeRegion = Convert.ToUInt64((string)data["home_region"]);
                if (data.Contains("home_region_id"))
                    userData.HomeRegionID = new UUID((string)data["home_region_id"]);
                else
                    userData.HomeRegionID = UUID.Zero;
                userData.HomeLocation =
                    new Vector3((float)Convert.ToDecimal((string)data["home_coordinates_x"]),
                                  (float)Convert.ToDecimal((string)data["home_coordinates_y"]),
                                  (float)Convert.ToDecimal((string)data["home_coordinates_z"]));
                userData.HomeLookAt =
                    new Vector3((float)Convert.ToDecimal((string)data["home_look_x"]),
                                  (float)Convert.ToDecimal((string)data["home_look_y"]),
                                  (float)Convert.ToDecimal((string)data["home_look_z"]));
                if (data.Contains("user_flags"))
                    userData.UserFlags = Convert.ToInt32((string)data["user_flags"]);
                if (data.Contains("god_level"))
                    userData.GodLevel = Convert.ToInt32((string)data["god_level"]);

                if (data.Contains("custom_type"))
                    userData.CustomType = (string)data["custom_type"];
                else
                    userData.CustomType = "";
                if (userData.CustomType == null)
                    userData.CustomType = "";

                if (data.Contains("partner"))
                    userData.Partner = new UUID((string)data["partner"]);
                else
                    userData.Partner = UUID.Zero;

                if (data.Contains("account"))
                    userData.account = (string)data["account"];
                else
                    userData.account = "";

                if (data.Contains("realname"))
                    userData.realname = (string)data["realname"];
                else
                    userData.realname = "";

                if (data.Contains("sessionHash"))
                    userData.sessionHash = (string)data["sessionHash"];
                else
                    userData.sessionHash = "";
                
                if (data.Contains("avatarStorageUrl"))
                    userData.avatarStorageUrl = (string)data["avatarStorageUrl"];
                else
                    userData.avatarStorageUrl = "";

                if (data.Contains("skypeUrl"))
                    userData.skypeUrl = (string)data["skypeUrl"];
                else
                    userData.account = "";

                if (data.Contains("gridUrl"))
                    userData.gridUrl = (string)data["gridUrl"];
                else
                    userData.gridUrl = "";

                RexUserAgentData agent = new RexUserAgentData();

                if (data.Contains("currentAgent"))
                {
                    Hashtable agentData = (Hashtable)data["currentAgent"];

                    agent.AgentIP = (string)agentData["agentIP"];
                    agent.AgentOnline = Convert.ToBoolean((string)data["agentOnline"]);
                    agent.AgentPort = Convert.ToUInt32((string)data["agentPort"]);
                    agent.Handle = Convert.ToUInt64((string)data["handle"]);
                    agent.InitialRegion = new UUID((string)data["initialRegion"]);
                    agent.LoginTime = Convert.ToInt32((string)data["loginTime"]);
                    agent.LogoutTime = Convert.ToInt32((string)data["logoutTime"]);
                    agent.LookAt = new Vector3((float)Convert.ToDecimal((string)data["home_look_x"]),
                                  (float)Convert.ToDecimal((string)data["home_look_y"]),
                                  (float)Convert.ToDecimal((string)data["home_look_z"]));

                    agent.Position = new Vector3((float)Convert.ToDecimal((string)data["currentPos_x"]),
                                  (float)Convert.ToDecimal((string)data["currentPos_y"]),
                                  (float)Convert.ToDecimal((string)data["currentPos_z"]));

                    agent.ProfileID = new UUID((string)data["UUID"]); ;
                    agent.Region = new UUID((string)data["regionID"]);
                    agent.SecureSessionID = new UUID((string)data["secureSessionID"]);
                    agent.SessionID = new UUID((string)data["sessionID"]);
                    agent.currentRegion = new UUID((string)data["currentRegion"]);

                    userData.CurrentAgent = agent;

                }

                return userData;

            }
            catch (Exception except)
            {
                m_log.Error("[AUTHENTICATION SERVICE]: ConvertXMLRPCDataToUserProfile", except);
            }

            return null;
            
        }


        private static List<AvatarPickerAvatar> ConvertXMLRPCDataToAvatarPickerList(UUID queryID, Hashtable data)
        {

            try
            {

            List<AvatarPickerAvatar> pickerlist = new List<AvatarPickerAvatar>();
            int pickercount = Convert.ToInt32((string)data["avcount"]);
            UUID respqueryID = new UUID((string)data["queryid"]);
            if (queryID == respqueryID)
            {
                for (int i = 0; i < pickercount; i++)
                {
                    AvatarPickerAvatar apicker = new AvatarPickerAvatar();
                    UUID avatarID = new UUID((string)data["avatarid" + i.ToString()]);
                    string firstname = (string)data["firstname" + i.ToString()];
                    string lastname = (string)data["lastname" + i.ToString()];
                    apicker.AvatarID = avatarID;
                    apicker.firstName = firstname;
                    apicker.lastName = lastname;
                    pickerlist.Add(apicker);
                }
            }
            else
            {
                m_log.Warn("[OGS1 USER SERVICES]: Got invalid queryID from userServer");
            }
            return pickerlist;

            }
            catch (Exception except)
            {
                m_log.Error("[AUTHENTICATION SERVICE]: ConvertXMLRPCDataToAvatarPickerList", except);
            }

            return null;

        }

        #endregion
    }
}
