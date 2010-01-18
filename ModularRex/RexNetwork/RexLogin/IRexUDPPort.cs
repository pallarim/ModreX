using System.Net;

namespace ModularRex.RexNetwork.RexLogin
{
    public interface IRexUDPPort
    {
        int GetPort(ulong regionHandle);
        int GetPort(IPEndPoint endPoint);
        bool RegisterRegionPort(ulong regionHandle, int port);
    }
}