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
    public delegate void RexStartUpDelegate(IRexClientCore remoteClient, UUID agentID, string status);
    public delegate void RexClientScriptCmdDelegate(IRexClientCore remoteClient, UUID agentID, List<string> parameters);

    #endregion

    public interface IRexClientCore
    {
        UUID AgentId { get; }

        string RexSkypeURL { get; set; }
        string RexAccount { get; set; }
        string RexAuthURL { get; set; }

        float RexCharacterSpeedMod { get; set; }
        float RexMovementSpeedMod { get; set; }
        float RexVertMovementSpeedMod { get; set; }
        bool RexWalkDisabled { get; set; }
        bool RexFlyDisabled { get; set; }

        event RexStartUpDelegate OnRexStartUp;
        event RexClientScriptCmdDelegate OnRexClientScriptCmd;
    }
}
