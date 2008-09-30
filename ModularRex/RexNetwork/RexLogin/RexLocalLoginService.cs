using System.Collections;
using Nwc.XmlRpc;
using OpenSim.Framework;
using OpenSim.Framework.Communications;
using OpenSim.Framework.Communications.Cache;
using OpenSim.Region.Communications.Local;

namespace ModularRex.RexNetwork.RexLogin
{
    class RexLocalLoginService : LocalLoginService 
    {
        public RexLocalLoginService(UserManagerBase userManager, string welcomeMess, IInterServiceInventoryServices interServiceInventoryService, LocalBackEndServices gridService, NetworkServersInfo serversInfo, bool authenticate, LibraryRootFolder libraryRootFolder) : base(userManager, welcomeMess, interServiceInventoryService, gridService, serversInfo, authenticate, libraryRootFolder)
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
