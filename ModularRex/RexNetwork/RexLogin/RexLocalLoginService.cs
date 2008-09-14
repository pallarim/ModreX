using System.Collections;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Region.Communications.Local;

namespace ModularRex.RexNetwork.RexLogin
{
    class RexLocalLoginService : LocalLoginService 
    {
        public RexLocalLoginService(UserManagerBase userManager, string welcomeMess, CommunicationsLocal parent, NetworkServersInfo serversInfo, bool authenticate) : base(userManager, welcomeMess, parent, serversInfo, authenticate)
        {
        }

        public override XmlRpcResponse XmlRpcLoginMethod(XmlRpcRequest request)
        {
            XmlRpcResponse retVal = base.XmlRpcLoginMethod(request);

            Hashtable response = (Hashtable)retVal.Value;

            response["rex"] = "running rex mode";
            response["message"] = "Connected to ModularRex Extensions";
            //response["sim_port"] = 9001; // RexPort

            return retVal;
        }

    }
}
