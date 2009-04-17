using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using OpenSim.Framework;
using OpenSim.Framework.Communications.Cache;

using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework.Scenes.Scripting;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.ScriptBase;
using log4net;
using OpenMetaverse;

namespace ModularRex.RexParts.RexPython
{
    public class RexScriptInterface : Rex_BuiltIn_Commands
    {
        private RexScriptEngine myScriptEngine;
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ModrexObjects m_rexObjects;

        public RexScriptInterface(IScriptEngine ScriptEngine, SceneObjectPart host, uint localID, UUID itemID, RexScriptEngine vScriptEngine)
        {   
            myScriptEngine = vScriptEngine;

            IScriptModule[] scriptModules = myScriptEngine.World.RequestModuleInterfaces<IScriptModule>();
            foreach (IScriptModule sm in scriptModules)
            {
                if (sm is IScriptEngine)
                {
                    IScriptEngine ise = (IScriptEngine)sm;
                    if (ise.ScriptEngineName == "ScriptEngine.DotNetEngine")
                    {
                        m_ScriptEngine = ise;
                        m_log.Info("[REXSCRIPT]: Found DotNetEngine");
                    }
                }
            }

            if (m_ScriptEngine == null)
            {
                m_log.Error("[REXSCRIPT]: Could not find DotNetEngine");
                throw new Exception("Could not find DotNetEngine");
            }
            //this was causing lots of errors. instead of creating a new instance of .Net script engine, check for an existing one and use that
            //this requires of using .NET scripting engine when using the python engine.
            //m_ScriptEngine = new OpenSim.Region.ScriptEngine.DotNetEngine.ScriptEngine();
            //m_ScriptEngine.World = myScriptEngine.World;
            try
            {
                base.Initialize(m_ScriptEngine, host, localID, itemID);
            }
            catch (Exception e)
            {
                m_log.Error("[REXSCRIPT]: Initializting rex scriptengine failed: " + e.ToString());
            }

            OpenSim.Region.Framework.Interfaces.IRegionModule module = myScriptEngine.World.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
            }
        }

        private EntityBase GetEntityBase(uint vId)
        {
            EntityBase entity;
            if (myScriptEngine.World.Entities.TryGetValue(vId, out entity))
            {
                if(entity is SceneObjectGroup)
                    return entity;
            }
            return null;
        }

        // Functions exposed to Python!
        // *********************************
        public bool SetScriptRunner(string vId)
        {
            uint id = System.Convert.ToUInt32(vId, 10);

            EntityBase entity;
            if (myScriptEngine.World.Entities.TryGetValue(id, out entity))
            {
                if (entity is SceneObjectGroup)
                {
                    SceneObjectGroup sog = (SceneObjectGroup)entity;
                    m_host = sog.RootPart;
                    m_localID = sog.LocalId;
                    m_itemID = sog.UUID;
                    return true;
                }
                m_log.DebugFormat("entity not scene object group: {0}", entity.GetType().ToString());
            }
            else
            {
                m_log.Debug("no entity found with id");
            }
            return false;
        }

        public void CommandToClient(string vPresenceId, string vUnit, string vCommand, string vCmdParams)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView client = (RexNetwork.RexClientView)temppre.ControllingClient;
                    client.SendRexScriptCommand(vUnit, vCommand, vCmdParams);
                }
            }
        }

        public bool GetPhysics(string vId)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
                return ((tempobj.ObjectFlags & (uint)PrimFlags.Physics) != 0);
            else
                return false;
        }

        public void SetPhysics(string vId, bool vbUsePhysics)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
            {
                if (vbUsePhysics)
                    tempobj.AddFlag(PrimFlags.Physics);
                else
                    tempobj.RemFlag(PrimFlags.Physics);

                tempobj.DoPhysicsPropertyUpdate(vbUsePhysics, false);
                tempobj.ScheduleFullUpdate();
            }
            else
                myScriptEngine.Log.WarnFormat("[PythonScript]: SetPhysics for nonexisting object:" + vId);     
        }

 
         
        public void SetMass(string vId, float vMass)
        {
            m_log.Warn("[REXSCRIPT]: SetMass not implemented");
            //SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            //if (tempobj != null)
            //{
            //    if (tempobj is RexObjects.RexObjectPart)
            //    {
            //        RexObjects.RexObjectPart rexObj = (RexObjects.RexObjectPart)tempobj;
            //        rexObj.SetMass(vMass);
            //    }
            //}
            //else
            //    myScriptEngine.Log.WarnFormat("[PythonScript]: SetMass for nonexisting object:" + vId); 
            
        }

        public void SetVelocity(string vId, LSL_Types.Vector3 vVelocity)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (((SceneObjectPart)tempobj) != null)
            {
                Vector3 tempvel = new Vector3((float)vVelocity.x, (float)vVelocity.y, (float)vVelocity.z);
                tempobj.Velocity = tempvel;
            }
        }

        public bool GetUsePrimVolumeCollision(string vId)
        {
            m_log.Warn("[REXSCRIPT]: GetUsePrimVolumeCollision not implemented");
            return false;
            //SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            //if (tempobj != null)
            //{
            //    if (tempobj is RexObjects.RexObjectPart)
            //    {
            //        RexObjects.RexObjectPart rexObj = (RexObjects.RexObjectPart)tempobj;
            //        return rexObj.GetUsePrimVolumeCollision();
            //    }
            //}
            //return false;
        }

        public void SetUsePrimVolumeCollision(string vId, bool vUseVolumeCollision)
        {
            m_log.Warn("[REXSCRIPT]: SetUsePrimVolumeCollision not implemented");
            //SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            //if (tempobj != null)
            //{
            //    if (tempobj is RexObjects.RexObjectPart)
            //    {
            //        RexObjects.RexObjectPart rexObj = (RexObjects.RexObjectPart)tempobj;
            //        rexObj.SetUsePrimVolumeCollision(vUseVolumeCollision);
            //    }
            //}
            //else
            //    myScriptEngine.Log.WarnFormat("[PythonScript]: SetPrimVolumeCollision for nonexisting object:" + vId);
        }

        public int GetPrimLocalIdFromUUID(string vUUID)
        {
            UUID tempid = UUID.Zero;
            try
            {
                tempid = new UUID(vUUID);
            }
            catch (Exception) { }
            
            if(tempid != UUID.Zero)            
            {
                EntityBase entity;
                if (myScriptEngine.World.Entities.TryGetValue(tempid, out entity))
                {
                    return (int)entity.LocalId;
                }
                else
                    myScriptEngine.Log.WarnFormat("[PythonScript]: GetPrimLocalIdFromUUID did not find prim with uuid:" + vUUID);
            }
            return 0;
        }



        // text messaging
        // ******************************
        public void SendGeneralAlertAll(string vId, string vMessage)
        {
            m_log.Warn("[REXSCRIPT]: SendGeneralAlertAll not implemented");
            //TODO: Fix this. Broken in newest OpenSim
            //myScriptEngine.World.SendGeneralAlert(vMessage);
        }

        public void SendAlertToAvatar(string vId,string vPresenceId, string vMessage, bool vbModal)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                temppre.ControllingClient.SendAgentAlertMessage(vMessage, vbModal);
            }
        }



        // Actor finding.
        public List<string> GetRadiusActors(string vId,float vRadius)
        {
            List<string> TempList = new List<string>();
            EntityBase tempobj = null;
            try
            {
                tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            }
            catch (Exception) { }
            try
            {
                if (tempobj == null)
                    tempobj = myScriptEngine.World.GetScenePresence(new UUID(vId));
            }
            catch (Exception) { }                               
            
            if (tempobj != null)
            {
                List<EntityBase> EntitiesList = myScriptEngine.World.GetEntities();
                foreach (EntityBase ent in EntitiesList) 
                {
                    if (ent is SceneObjectGroup || ent is ScenePresence)
                    {
                        if (Util.GetDistanceTo(ent.AbsolutePosition, tempobj.AbsolutePosition) < vRadius)
                            TempList.Add(ent.LocalId.ToString());
                    }
                }
            }
            return TempList;
        }

        public List<string> GetRadiusAvatars(string vId, float vRadius)
        {
            List<string> TempList = new List<string>();
            EntityBase tempobj = null;
            
            try
            {
                tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            }
            catch(Exception) { }
            try
            {
                if (tempobj == null)
                    tempobj = myScriptEngine.World.GetScenePresence(new UUID(vId));
            }
            catch (Exception) { }
            
            if (tempobj != null)
            {
                List<EntityBase> EntitiesList = myScriptEngine.World.GetEntities();
                foreach (EntityBase ent in EntitiesList) 
                {
                    if (ent is ScenePresence)
                    {
                        if (Util.GetDistanceTo(ent.AbsolutePosition, tempobj.AbsolutePosition) < vRadius)
                            TempList.Add(ent.LocalId.ToString());
                    }
                }
            }
            return TempList;
        }



        public string SpawnActor(LSL_Types.Vector3 location, int shape, bool temporary, string pythonClass)
        {
            UUID TempID = myScriptEngine.World.RegionInfo.MasterAvatarAssignedUUID;
            Vector3 pos = new Vector3((float)location.x, (float)location.y, (float)location.z);
            Quaternion rot = new Quaternion(0.0f, 0.0f, 0.0f, 1.0f);

            SceneObjectGroup sog = myScriptEngine.World.AddNewPrim(TempID, TempID, pos, rot, GetShape(shape));
            uint AddResult = sog.RootPart.LocalId;

            ModularRex.RexFramework.RexObjectProperties rop = m_rexObjects.GetObject(sog.RootPart.UUID);
            rop.RexClassName = pythonClass;

            //TODO: vbTemporary
            //uint AddResult = myScriptEngine.World.AddNewPrimReturningId(TempID, pos, rot, GetShape(vShape), vbTemporary, vPyClass);
            return AddResult.ToString();
        }

        public bool DestroyActor(string vId)
        {
            m_log.Warn("[REXSCRIPT]: DestroyActor not implemented");
            return true;
            //EntityBase tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            //if (tempobj != null && tempobj is RexObjects.RexObjectGroup)
            //{
            //    ((RexObjects.RexObjectGroup)tempobj).DeleteMe = true; // Do not call DeleteSceneObjectGroup for deleting directly
            //    return true;
            //}
            //return false;
        }
          
        private static PrimitiveBaseShape GetShape(int vShape)
        {
            PrimitiveBaseShape shape = new PrimitiveBaseShape();
            
            shape.PCode = 9;
            shape.PathBegin = 0;
            shape.PathEnd = 0;
            shape.PathScaleX = 100;
            shape.PathScaleY = 100;
            shape.PathShearX = 0;
            shape.PathShearY = 0;
            shape.PathSkew = 0;
            shape.ProfileBegin = 0;
            shape.ProfileEnd = 0;
            shape.Scale = new Vector3(0.5f, 0.5f, 0.5f);
            //shape.Scale.X = shape.Scale.Y = shape.Scale.Z = 0.5f;
            shape.PathCurve = 16;
            shape.ProfileCurve = 1;
            shape.ProfileHollow = 0;
            shape.PathRadiusOffset = 0;
            shape.PathRevolutions = 0;
            shape.PathTaperX = 0;
            shape.PathTaperY = 0;
            shape.PathTwist = 0;
            shape.PathTwistBegin = 0;
            Primitive.TextureEntry ntex = new Primitive.TextureEntry(new UUID("00000000-0000-1111-9999-000000000005"));
            shape.TextureEntry = ntex.GetBytes();//ntex.ToBytes(); 
            return shape;
        }

        // Scenepresence related
        // These are now in RexClientView, although most of them are not working properly yet.

        public string SPGetFullName(string vPresenceId)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                string TempString = temppre.Firstname + " " + temppre.Lastname;
                return TempString;
            }
            else
                return "";
        }
        public string SPGetFirstName(string vPresenceId)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
                return temppre.Firstname;           
            else
                return "";
        }
        public string SPGetLastName(string vPresenceId)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
                return temppre.Lastname;
            else
                return "";
        }

        public void SPDoLocalTeleport(string vPresenceId, LSL_Types.Vector3 vLocation)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                Vector3 position = new Vector3((float)vLocation.x, (float)vLocation.y, (float)vLocation.z);
                Vector3 lookAt = new Vector3(0,0,0);
                temppre.ControllingClient.SendTeleportLocationStart();
                temppre.ControllingClient.SendLocalTeleport(position, lookAt,0);
                temppre.Teleport(position);
            }
        }

        public float SPGetMovementModifier(string vPresenceId)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    return rexclient.RexMovementSpeedMod;
                }
            }
            return 0.0f;
        }

        public void SPSetMovementModifier(string vPresenceId,float vSpeedModifier)
        {
         
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    rexclient.RexMovementSpeedMod = vSpeedModifier;
                }
            }
        }

        public LSL_Types.Vector3 SPGetPos(string vPresenceId)
        {
            LSL_Types.Vector3 loc = new LSL_Types.Vector3(0, 0, 0);

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                loc.x = temppre.AbsolutePosition.X;
                loc.y = temppre.AbsolutePosition.Y;
                loc.z = temppre.AbsolutePosition.Z;
            }
            return loc;
        }

        public LSL_Types.Quaternion SPGetRot(string vPresenceId)
        {
            LSL_Types.Quaternion rot = new LSL_Types.Quaternion(0, 0, 0, 1);

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                rot.x = temppre.Rotation.X;
                rot.y = temppre.Rotation.Y;
                rot.z = temppre.Rotation.Z;
                rot.s = temppre.Rotation.W;
            }
            return rot;
        }

        public void SPSetRot(string vPresenceId,LSL_Types.Quaternion vRot, bool vbRelative)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView client = (RexNetwork.RexClientView)temppre.ControllingClient;
                    string sparams = vRot.x.ToString() + " " + vRot.y.ToString() + " " + vRot.z.ToString() + " " + vRot.s.ToString();
                    sparams = sparams.Replace(",", ".");
                    if (vbRelative)
                        client.SendRexScriptCommand("client", "setrelrot", sparams);
                    else
                    {
                        temppre.Rotation = new Quaternion((float)vRot.x, (float)vRot.y, (float)vRot.z, (float)vRot.s);
                        temppre.UpdateMovement();
                        //client.SendRexScriptCommand("client", "setrot", sparams);
                    }
                }
            }    
        }

        public bool SPGetWalkDisabled(string vPresenceId)
        {

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    return rexclient.RexWalkDisabled;
                }
            }
            return false;
        }

        public void SPSetWalkDisabled(string vPresenceId, bool vbValue)
        {

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    rexclient.RexWalkDisabled = vbValue;
                }
            }
        }

        public bool SPGetFlyDisabled(string vPresenceId)
        {

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    return rexclient.RexFlyDisabled;
                }
            }
            return false;
        }

        public void SPSetFlyDisabled(string vPresenceId, bool vbValue)
        {

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    rexclient.RexFlyDisabled = vbValue;
                }
            }
        }

        public float SPGetVertMovementModifier(string vPresenceId)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
            {
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    return rexclient.RexVertMovementSpeedMod;
                }
            }
            return 0.0f;
        }

        public void SPSetVertMovementModifier(string vPresenceId, float vSpeedModifier)
        {

            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null)
                if (temppre.ControllingClient is RexNetwork.RexClientView)
                {
                    RexNetwork.RexClientView rexclient = (RexNetwork.RexClientView)temppre.ControllingClient;
                    rexclient.RexVertMovementSpeedMod = vSpeedModifier;
                }
        }


        public bool SPGetSitDisabled(string vPresenceId)
        {

            UUID avatarID = new UUID(vPresenceId);
            ISitMod mod = m_ScriptEngine.World.RequestModuleInterface<ISitMod>();
            return mod.GetSitDisabled(avatarID);
        }

        public void SPSetSitDisabled(string vPresenceId, bool vbValue)
        {

            UUID avatarID = new UUID(vPresenceId);
            ISitMod mod = m_ScriptEngine.World.RequestModuleInterface<ISitMod>();
            mod.SetSitDisabled(avatarID, vbValue);
        }


        // Rexbot related
        public void BotWalkTo(string vPresenceId, LSL_Types.Vector3 vDest)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                Vector3 dest = new Vector3((float)vDest.x, (float)vDest.y, (float)vDest.z);
                (temppre.ControllingClient as IRexBot).WalkTo(dest);
            }


        }

        public void BotFlyTo(string vPresenceId, LSL_Types.Vector3 vDest)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                Vector3 dest = new Vector3((float)vDest.x, (float)vDest.y, (float)vDest.z);
                (temppre.ControllingClient as IRexBot).FlyTo(dest);
            }
        }

        public void BotRotateTo(string vPresenceId, LSL_Types.Vector3 vTarget)
        {
            UUID TempId = new UUID(vPresenceId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                Vector3 dest = new Vector3((float)vTarget.x, (float)vTarget.y, (float)vTarget.z);
                (temppre.ControllingClient as IRexBot).RotateTo(dest);
            }
        }

        /// <summary>
        /// deprecated. prefer BotPauseAutoMove() or BotStopAutoMove()
        /// </summary>
        [Obsolete("use BotPauseAutoMove() or BotStopAutoMove()")]
        public void BotEnableAutoMove(string botId, bool vEnable)
        {
            UUID TempId = new UUID(botId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                (temppre.ControllingClient as IRexBot).EnableAutoMove(vEnable, true);
            }
        }

        /// <summary>
        /// Temporarily pause bot auto movement. bot will still warp to destination if it is deemed stuck
        /// </summary>
        /// <param name="botId">UUID of the bots ScenePresence</param>
        /// <param name="vEnable"></param>
        public void BotPauseAutoMove(string botId, bool vEnable)
        {
            UUID TempId = new UUID(botId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                (temppre.ControllingClient as IRexBot).PauseAutoMove(vEnable);
            }
        }

        /// <summary>
        /// Stop/start bot auto movement. Bot will not warp to destination after it has been stopped,
        /// no logic for checking if bot is stuck
        /// </summary>
        /// <param name="botId">UUID of the bots ScenePresence</param>
        /// <param name="vEnable"></param>
        public void BotStopAutoMove(string botId, bool vEnable)
        {
            UUID TempId = new UUID(botId);
            ScenePresence temppre = myScriptEngine.World.GetScenePresence(TempId);
            if (temppre != null && temppre.ControllingClient is IRexBot)
            {
                (temppre.ControllingClient as IRexBot).StopAutoMove(vEnable);
            }
        }
        

        #region Functions not supported at the moment.
        /*  
        public bool GetFreezed(string vId)
        {
            EntityBase tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
            {
                return tempobj.IsFreezed;
            }
            else
            {
                return false;
            }
             
            return false;
        }

        public void SetFreezed(string vId, bool vbFreeze)
        {
            EntityBase tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
            {
                tempobj.IsFreezed = vbFreeze;
                if (tempobj is ScenePresence && vbFreeze)
                    ((ScenePresence)tempobj).rxStopAvatarMovement();
            }
             
        } */

        /* 
        public int GetPhysicsMode(string vId)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
                return tempobj.GetPhysicsMode();
            else
                return 0;
             
            return 0;
        }

        public void SetPhysicsMode(string vId, int vPhysicsMode)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
            {
                tempobj.SetPhysicsMode(vPhysicsMode);
            }
            else
                myScriptEngine.Log.Verbose("PythonScript", "SetPhysicsMode for nonexisting object:" + vId); 
        }
        */


        /* 
        public bool GetUseGravity(string vId)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
                return tempobj.GetUseGravity();
            else
                return false;
            
            return false;
        }

        public void SetUseGravity(string vId, bool vbUseGravity)
        {
            SceneObjectPart tempobj = myScriptEngine.World.GetSceneObjectPart(System.Convert.ToUInt32(vId, 10));
            if (tempobj != null)
                tempobj.SetUseGravity(vbUseGravity);
            else
                myScriptEngine.Log.Verbose("PythonScript", "SetUseGravity for nonexisting object:" + vId);     
        }
        */

        /* 
        public void SetLocationFast(string vId,rxVector vLoc)
        {
            EntityBase tempobj = GetEntityBase(System.Convert.ToUInt32(vId, 10));
            if (((SceneObjectGroup)tempobj) != null)
            {
                bool hasPrim = ((SceneObjectGroup)tempobj).HasChildPrim(tempobj.UUID);
                if (hasPrim != false)
                {
                    LLVector3 TempLoc = new LLVector3((float)vLoc.x, (float)vLoc.y, (float)vLoc.z);
                    LLVector3 TempOffset = new LLVector3(0, 0, 0);
                    ((SceneObjectGroup)tempobj).GrabMovement(TempOffset, TempLoc, null); // tucofixme, might break some day, because sending null remoteClient parameter
                }
            }
        }
        */
        #endregion

        public float TimeOfDay //is double in LL interface, but float inside opensim estate info
        {
            get
            {
                return (float)myScriptEngine.World.RegionInfo.EstateSettings.SunPosition;//sunHour; //llGetTimeOfDay();
            }

            set
            {
                myScriptEngine.World.RegionInfo.EstateSettings.SunPosition = value;
                IEstateModule estate = myScriptEngine.World.RequestModuleInterface<IEstateModule>();
                if (estate is OpenSim.Region.CoreModules.World.Estate.EstateManagementModule)
                    ((OpenSim.Region.CoreModules.World.Estate.EstateManagementModule)estate).sendRegionInfoPacketToAll();
            }
        }
    }
}









