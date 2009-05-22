using OpenMetaverse;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// This client supports realXtend style client appearances
    /// </summary>
    public interface IClientRexAppearance
    {
        event RexAppearanceDelegate OnRexAppearance;
        void SendRexAppearance(UUID agentID, string avatarURL, bool overrideUsed);

        string RexAvatarURL { get; set; }
        string RexAvatarURLOverride { get; set; }
        string RexAvatarURLVisible { get; }
    }
}