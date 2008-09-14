using OpenMetaverse;

namespace ModularRex.RexNetwork
{
    public interface IClientRexAppearance
    {
        event RexAppearanceDelegate OnRexAppearance;
        void SendRexAppearance(UUID agentID, string avatarURL);
    }
}