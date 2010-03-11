using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Services.LLLoginService;
using System.Collections;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenMetaverse;
using System.Net;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using FriendInfo = OpenSim.Services.Interfaces.FriendInfo;

namespace ModularRex.RexNetwork.RexLogin
{
    public class RexFailedLoginResponse : LLFailedLoginResponse
    {
        public static RexFailedLoginResponse SessionProblem;
        public static RexFailedLoginResponse AccountCreationProblem;
        public static RexFailedLoginResponse DeniedAccount;

        static RexFailedLoginResponse()
        {
            SessionProblem = new RexFailedLoginResponse("key", "Login service failed to verify users session", "false");
            AccountCreationProblem = new RexFailedLoginResponse("key", "Failed to create local account for user", "false");
            DeniedAccount = new RexFailedLoginResponse("key", "User account not allowed to login", "false");

        }

        public RexFailedLoginResponse(string key, string value, string login) : base(key, value, login)
        {
        }
    }

    public class RexLoginResponse : LLLoginResponse
    {
        public RexLoginResponse(UserAccount account, AgentCircuitData aCircuit, PresenceInfo pinfo,
            GridRegion destination, List<InventoryFolderBase> invSkel, FriendInfo[] friendsList, ILibraryService libService,
            string where, string startlocation, Vector3 position, Vector3 lookAt, string message,
            GridRegion home, IPEndPoint clientIP)
            : base(account, aCircuit, pinfo, destination, invSkel, friendsList, libService, where, startlocation, position, lookAt, message, home, clientIP)
        {
        }

        public override Hashtable ToHashtable()
        {
            Hashtable responseData = base.ToHashtable();
            responseData["rex"] = "running rex mode";
            return responseData;
        }
    }
}
