using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Framework;
using ModularRex.RexNetwork;
using OpenMetaverse;

namespace ModularRex.RexParts.RexPython
{
    /* tucofixme, delete?
    public delegate void ObjectClickAction(IClientAPI remoteClient, uint objectLocalId, byte clickAction);
    public delegate void ReceiveRexClientScriptCmd(IClientAPI remoteClient, UUID agentID, List<string> vParams); // rex
    public delegate void ReceiveRexFaceExpression(IClientAPI remoteClient, UUID agentID, List<string> expressionData); // rex
    public delegate void ReceiveRexIKAnimation(IClientAPI remoteClient, UUID agentID, List<string> animationData); // rex
    public delegate void ReceiveRexAvatarProp(IClientAPI remoteClient, UUID agentID, List<string> vParams); // rex
    public delegate void ReceiveRexSkypeStore(IClientAPI remoteClient, UUID agentID, List<string> vParams); // rex
    public delegate void ReceiveRexStartUp(IClientAPI remoteClient, UUID agentID, string vStatus); // rex
    public delegate void TriggerSound(IClientAPI remoteClient, UUID soundID, UUID ownerID, UUID objectID, UUID parentID, ulong handle, Vector3 position, float gain); //rex
    public delegate void ReceiveRexMediaURL(IClientAPI remoteClient, UUID agentID, UUID vItemID, string vMediaURL, byte vRefreshRate); // rex    
    public delegate void UpdateRexData(IClientAPI remoteClient, UUID vPrimID, string vData);
    public delegate void UpdateRexPrimData(IClientAPI remoteClient, UUID vPrimID, byte[] vData);

    public delegate void SendAppearanceToAllAgents(); // rex
    */

    public interface IRexBot
    {
        /* tucofixme, delete?
        event ReceiveRexClientScriptCmd OnReceiveRexClientScriptCmd; // rex
        event ReceiveRexFaceExpression OnReceiveRexFaceExpression; // rex
        event ReceiveRexIKAnimation OnReceiveRexIKAnimation; // rex
        event UpdateRexData OnUpdateRexData; // rex
        event UpdateRexPrimData OnUpdateRexPrimData; // rex

        event SendAppearanceToAllAgents OnAppearanceUpdate; // rex
        event ReceiveRexAvatarProp OnReceiveRexAvatarProp; // rex
        event ReceiveRexSkypeStore OnReceiveRexSkypeStore; // rex
        event ReceiveRexStartUp OnReceiveRexStartUp; // rex
        event ReceiveRexMediaURL OnReceiveRexMediaURL; // rex
        event TriggerSound OnTriggerSound; // rex
        */ 
        void WalkTo(Vector3 destination);
        void FlyTo(Vector3 destination);
        void RotateTo(Vector3 destination);
        void EnableAutoMove(bool enable, bool stopWarpTimer);
        void StopAutoMove(bool stop);
        void PauseAutoMove(bool pause);
    }
}
