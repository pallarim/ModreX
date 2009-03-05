using System;
using System.Collections.Generic;
using OpenMetaverse;
using OpenSim.Framework;
using ModularRex.RexFramework;

namespace ModularRex.RexNetwork
{
    #region Rex ClientView delegate definitions

    public delegate void RexGenericMessageDelegate(IClientAPI sender, List<string> parameters);
    public delegate void RexAppearanceDelegate(IClientAPI sender);
    public delegate void RexObjectPropertiesDelegate(IClientAPI sender, UUID id, RexObjectProperties props);
    public delegate void RexStartUpDelegate(IClientAPI remoteClient, UUID agentID, string status);
    public delegate void RexClientScriptCmdDelegate(IClientAPI remoteClient, UUID agentID, List<string> parameters);
    public delegate void ReceiveRexMediaURL(IClientAPI remoteClient, UUID agentID, UUID itemID, string mediaURL, byte refreshRate);

    #endregion

    public interface IRexClientAPI : IClientRexFaceExpression, IClientRexAppearance, IClientMediaURL
    {
        UUID AgentId { get; }
    
        string RexAvatarURL { get; set; }
        string RexAvatarURLOverride { get; set; }
        string RexAvatarURLVisible { get; }
        string RexSkypeURL { get; set; }
        string RexAccount { get; set; }
        string RexAuthURL { get; set; }

        float RexCharacterSpeedMod { get; set; }
        float RexMovementSpeedMod { get; set; }
        float RexVertMovementSpeedMod { get; set; }
        bool RexWalkDisabled { get; set; }
        bool RexFlyDisabled { get; set; }
        bool RexSitDisabled { get; set; }
    }
}
