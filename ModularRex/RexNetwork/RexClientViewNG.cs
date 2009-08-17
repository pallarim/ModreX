using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using ModularRex.RexFramework;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.ClientStack.LindenUDP;
using OpenSim.Region.ClientStack;
using OpenMetaverse.Packets;

namespace ModularRex.RexNetwork
{
    //TODO: RENAME!
    public class RexClientViewNG : RexClientViewBase
    {
        public RexClientViewNG(EndPoint remoteEP, IScene scene, IAssetCache assetCache,
                             LLPacketServer packServer, AuthenticateResponse authenSessions, UUID agentId,
                             UUID sessionId, uint circuitCode, EndPoint proxyEP, ClientStackUserSettings userSettings)
            : base(remoteEP, scene, packServer, authenSessions, agentId,
                   sessionId, circuitCode, proxyEP, userSettings)
        {
            OnBinaryGenericMessage -= base.RexClientView_BinaryGenericMessage;
            OnBinaryGenericMessage += ng_BinaryGenericMessage;
        }

        protected void ng_BinaryGenericMessage(Object sender, string method, byte[][] args)
        {
            if (method.ToLower() == "RexPrimData".ToLower())
            {
                HandleRexPrimData(args);
                return;
            }
        }

        private void HandleRexPrimData(byte[][] args)
        {
            int rpdLen = 0;
            int idx = 0;
            bool first = false;
            UUID id = UUID.Zero;

            foreach (byte[] arg in args)
            {
                if (!first)
                {
                    id = new UUID(Util.FieldToString(arg));
                    first = true;
                    continue;
                }

                rpdLen += arg.Length;
            }

            first = false;
            byte[] rpdArray = new byte[rpdLen];

            foreach (byte[] arg in args)
            {
                if (!first)
                {
                    first = true;
                    continue;
                }

                arg.CopyTo(rpdArray, idx);
                idx += arg.Length;
            }

            TriggerOnRexObjectProperties(this, id, new RexObjectProperties(rpdArray, true));
        }

        public override void SendRexObjectProperties(UUID id, RexObjectProperties x)
        {
            GenericMessagePacket gmp = new GenericMessagePacket();
            gmp.MethodData.Method = Utils.StringToBytes("RexPrimData");

            byte[] temprexprimdata = x.GetRexPrimDataToBytes(true); //send urls to ng-clients
            int numlines = 0;
            int i = 0;

            if (temprexprimdata != null)
            {
                while (i <= temprexprimdata.Length)
                {
                    numlines++;
                    i += 200;
                }
            }

            gmp.ParamList = new GenericMessagePacket.ParamListBlock[1 + numlines];
            gmp.ParamList[0] = new GenericMessagePacket.ParamListBlock();
            gmp.ParamList[0].Parameter = Utils.StringToBytes(id.ToString());

            for (i = 0; i < numlines; i++)
            {
                gmp.ParamList[i + 1] = new GenericMessagePacket.ParamListBlock();

                if ((temprexprimdata.Length - i * 200) < 200)
                {
                    gmp.ParamList[i + 1].Parameter = new byte[temprexprimdata.Length - i * 200];
                    Buffer.BlockCopy(temprexprimdata, i * 200, gmp.ParamList[i + 1].Parameter, 0, temprexprimdata.Length - i * 200);
                }
                else
                {
                    gmp.ParamList[i + 1].Parameter = new byte[200];
                    Buffer.BlockCopy(temprexprimdata, i * 200, gmp.ParamList[i + 1].Parameter, 0, 200);
                }
            }

            OutPacket(gmp, ThrottleOutPacketType.Task);
        }
    }
}
