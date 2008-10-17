using OpenSim.Region.ClientStack;
using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public class RexUDPServer : LLUDPServer 
    {
        protected override void CreatePacketServer(ClientStackUserSettings userSettings)
        {
            new RexPacketServer(this, userSettings);
        }
    }
}
