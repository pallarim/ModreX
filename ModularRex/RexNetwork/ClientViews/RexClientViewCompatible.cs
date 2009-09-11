using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using OpenMetaverse.Packets;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.ClientStack;

namespace ModularRex.RexNetwork
{
    /// <summary>
    /// This client view is ment for realXtend clients and it is compatible with LLClient.
    /// This client view should not crash LL clients or their derivants.
    /// </summary>
    public class RexClientViewCompatible : RexClientViewBase
    {
        public RexClientViewCompatible(EndPoint remoteEP, IScene scene,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
        }

        public override void SendAvatarTerseUpdate(ulong regionHandle,
                ushort timeDilation, uint localID, Vector3 position,
                Vector3 velocity, Quaternion rotation, UUID agentid)
        {
            if (rotation.X == rotation.Y &&
                rotation.Y == rotation.Z &&
                rotation.Z == rotation.W && rotation.W == 0)
                rotation = Quaternion.Identity;

            position.Z = (float)(position.Z - 0.15);

            ImprovedTerseObjectUpdatePacket.ObjectDataBlock terseBlock =
                CreateAvatarImprovedBlock(localID, position, velocity, rotation);

            lock (m_avatarTerseUpdates)
            {
                m_avatarTerseUpdates.Add(terseBlock);

                // If packet is full or own movement packet, send it.
                if (m_avatarTerseUpdates.Count >= m_avatarTerseUpdatesPerPacket)
                {
                    ProcessAvatarTerseUpdates(this, null);
                }
                else if (m_avatarTerseUpdates.Count == 1)
                {
                    m_avatarTerseUpdateTimer.Start();
                }
            }
        }
    }
}
