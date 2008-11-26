using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// Extends the standard OpenSim UDP Server Class
    /// With the only difference being that the packet
    /// server spawns RexClientView instances instead
    /// of LLClientView's.
    /// </summary>
    public class RexUDPServer : LLUDPServer 
    {
        protected override void CreatePacketServer(ClientStackUserSettings userSettings)
        {
            new RexPacketServer(this, userSettings);
        }
    }
}
