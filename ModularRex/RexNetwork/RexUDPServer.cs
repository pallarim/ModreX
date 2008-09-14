using OpenSim.Region.ClientStack.LindenUDP;

namespace ModularRex.RexNetwork
{
    public class RexUDPServer : LLUDPServer 
    {
        protected override void CreatePacketServer()
        {
            new RexPacketServer(this);
        }
    }
}
