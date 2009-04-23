using System.Net;

namespace ModularRex.RexNetwork.RexLogin
{
    public interface IRexUDPPort
    {
        int GetPort(ulong regionHandle);
        int GetPort(IPEndPoint endPoint);
    }
}