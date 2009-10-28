using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.ClientStack;
using OpenSim.Region.Framework.Scenes;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// This client view is ment for realXtend clients and it is compatible with LLClient.
    /// This client view should not crash LL clients or their derivants.
    /// </summary>
    public class RexClientViewCompatible : RexClientViewBase
    {
        public RexClientViewCompatible(EndPoint remoteEP, Scene scene,
                             LLUDPServer udpServer, LLUDPClient udpClient, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode)
            : base(remoteEP, scene, udpServer, udpClient, authenSessions, agentId,
                   sessionId, circuitCode)
        {
        }

        public override void SendAvatarTerseUpdate(SendAvatarTerseData data)
        {
            if (data.Priority == double.NaN)
            {
                //m_log.Error("[LLClientView] SendAvatarTerseUpdate received a NaN priority, dropping update");
                return;
            }

            Quaternion rotation = data.Rotation;
            if (rotation.W == 0.0f && rotation.X == 0.0f && rotation.Y == 0.0f && rotation.Z == 0.0f)
                rotation = Quaternion.Identity;

            ImprovedTerseObjectUpdatePacket.ObjectDataBlock terseBlock = CreateImprovedTerseBlock(data);

            lock (m_avatarTerseUpdates.SyncRoot)
                m_avatarTerseUpdates.Enqueue(data.Priority, terseBlock, data.LocalID);

            // If we received an update about our own avatar, process the avatar update priority queue immediately
            if (data.AgentID == m_agentId)
                ProcessAvatarTerseUpdates();
        }

        //public override void SendAvatarTerseUpdate(ulong regionHandle,
        //        ushort timeDilation, uint localID, Vector3 position,
        //        Vector3 velocity, Quaternion rotation, UUID agentid)
        //{
        //    if (rotation.X == rotation.Y &&
        //        rotation.Y == rotation.Z &&
        //        rotation.Z == rotation.W && rotation.W == 0)
        //        rotation = Quaternion.Identity;

        //    position.Z = (float)(position.Z - 0.15);

        //    ImprovedTerseObjectUpdatePacket.ObjectDataBlock terseBlock =
        //        CreateAvatarImprovedBlock(localID, position, velocity, rotation);

        //    lock (m_avatarTerseUpdates)
        //    {
        //        m_avatarTerseUpdates.Add(terseBlock);

        //        // If packet is full or own movement packet, send it.
        //        if (m_avatarTerseUpdates.Count >= m_avatarTerseUpdatesPerPacket)
        //        {
        //            ProcessAvatarTerseUpdates(this, null);
        //        }
        //        else if (m_avatarTerseUpdates.Count == 1)
        //        {
        //            m_avatarTerseUpdateTimer.Start();
        //        }
        //    }
        //}
    }
}
