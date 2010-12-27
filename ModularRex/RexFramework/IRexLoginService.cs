using System.Net;
using OpenMetaverse;
using OpenSim.Services.Interfaces;

namespace ModularRex.RexFramework
{
    public interface IRexLoginService : ILoginService
    {
        LoginResponse Login(string account, string sessionHash, string startLocation, UUID scopeID, string clientVersion, IPEndPoint clientIP, string channel, string mac, string id0);
    }
}
