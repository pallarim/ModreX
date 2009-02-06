// rex
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using OpenMetaverse;
using OpenSim.Region.Environment.Scenes;
using OpenSim.Framework;
using OpenSim.Region.Interfaces;
using OpenSim.Region.ScriptEngine.Shared;
using OpenSim.Region.ScriptEngine.Interfaces;
using OpenSim.Region.ScriptEngine.Shared.Api;
using log4net;
using ModularRex.RexFramework;

namespace ModularRex.RexParts
{
    public class Rex_BuiltIn_Commands : LSL_Api, Rex_BuiltIn_Commands_Interface
    {
        private static readonly ILog m_log =
            LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ModrexObjects m_rexObjects;
        private bool m_automaticLinkPermission = false;
        private IMessageTransferModule m_TransferModule = null;

        public new void Initialize(IScriptEngine ScriptEngine, SceneObjectPart host, uint localID, UUID itemID)
        {
            m_ScriptEngine = ScriptEngine;
            m_host = host;
            m_localID = localID;
            m_itemID = itemID;

            m_ScriptDelayFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDelayFactor", 1.0f);
            m_ScriptDistanceFactor =
                m_ScriptEngine.Config.GetFloat("ScriptDistanceLimitFactor", 1.0f);
            m_MinTimerInterval =
                m_ScriptEngine.Config.GetFloat("MinTimerInterval", 0.5f);
            m_automaticLinkPermission =
                m_ScriptEngine.Config.GetBoolean("AutomaticLinkPermission", false);

            m_TransferModule =
                    m_ScriptEngine.World.RequestModuleInterface<IMessageTransferModule>();
            AsyncCommands = new AsyncCommandManager(ScriptEngine);

            OpenSim.Region.Environment.Interfaces.IRegionModule module = World.Modules["RexObjectsModule"];
            if (module != null && module is ModrexObjects)
            {
                m_rexObjects = (ModrexObjects)module;
            }
        }

        //public void Initialize(IScriptEngine scriptEngine, SceneObjectPart host, uint localID, UUID itemID)
        //{
        //    try
        //    {
        //        base.Initialize(scriptEngine, host, localID, itemID);
        //    }
        //    catch (Exception e)
        //    {
        //        m_log.Error("[REXSCRIPT]: Initializting rex scriptengine failed: " + e.ToString());
        //    }
        //}

        /* are in db for assets, but in UI for textures only - also this now works for just textures 
           TODO: options for which faces to affect, e.g. main &/ some individual faces */
        public int rexSetTextureMediaURL(string url)
        {
            return rexSetTextureMediaURL(url, 0);
        }

        // This function sets the mediaurl for all textures which are in the prim to the param...
        public int rexSetTextureMediaURL(string url, int vRefreshRate)
        {
            int changed = 0;

            Primitive.TextureEntry texs = m_host.Shape.Textures;
            Primitive.TextureEntryFace texface;

            if (tryTextureMediaURLchange(texs.DefaultTexture, url, (byte)vRefreshRate))
                changed++;

            for (uint i = 0; i < 32; i++)
            {
                if (texs.FaceTextures[i] != null)
                {
                    texface = texs.FaceTextures[i];
                    //made based on the example in llPlaySound, which seems to be the only prev thing on assets         
                    //Console.WriteLine("Changing texture " + texface.TextureID.ToString());
                    if (tryTextureMediaURLchange(texface, url, (byte)vRefreshRate))
                        changed++;
                }
            }
            return changed; //number of textures changed. usually 1 i guess?
        }

        //the guts of the api method below
        private bool tryTextureMediaURLchange(Primitive.TextureEntryFace texface, string url, byte vRefreshRate)
        {
            AssetBase texasset;

            texasset = World.AssetCache.GetAsset(texface.TextureID, true);
            if (texasset != null)
            {
                World.ForEachScenePresence(delegate(ScenePresence controller)
                {
                    if (controller.ControllingClient is RexNetwork.RexClientView)
                    {
                        ((RexNetwork.RexClientView)controller.ControllingClient).SendMediaURL(texface.TextureID, url, vRefreshRate);
                    }
                });
                //Old Rex: World.UpdateAssetMediaURLRequest(texface.TextureID, texasset, url, vRefreshRate);
                return true;
            }
            else
            {
                return false;
            }

        }



        public void rexIKSetLimbTarget(string vAvatar, int vLimbId, LSL_Types.Vector3 vDest, float vTimeToTarget, float vStayTime, float vConstraintAngle, string vStartAnim, string vTargetAnim, string vEndAnim) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    Vector3 targetpos = new Vector3((float)vDest.x, (float)vDest.y, (float)vDest.z);
                    World.ForEachScenePresence(delegate(ScenePresence controller)
                    {
                        if (controller.ControllingClient is RexNetwork.RexClientView)
                        {
                            ((RexNetwork.RexClientView)controller.ControllingClient).RexIKSendLimbTarget(target.UUID, vLimbId, targetpos, vTimeToTarget, vStayTime, vConstraintAngle, vStartAnim, vTargetAnim, vEndAnim);
                        }
                    });
                    //World.SendRexIKSetLimbTargetToAll(target.UUID, vLimbId, targetpos, vTimeToTarget, vStayTime, vConstraintAngle, vStartAnim, vTargetAnim, vEndAnim);
                }
            }
            catch { }
        }

        public void rexPlayAvatarAnim(string vAvatar, string vAnimName, float vRate, float vFadeIn, float vFadeOut, int nRepeats, bool vbStopAnim) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    World.ForEachScenePresence(delegate(ScenePresence controller)
                    {
                        if (controller.ControllingClient is RexNetwork.RexClientView)
                        {
                            ((RexNetwork.RexClientView)controller.ControllingClient).SendRexAvatarAnimation(target.UUID, vAnimName, vRate, vFadeIn, vFadeOut, nRepeats, vbStopAnim);
                        }
                    });
                    //World.SendRexPlayAvatarAnimToAll(target.UUID, vAnimName, vRate, vFadeIn, vFadeOut, nRepeats, vbStopAnim);
                }
            }
            catch { }
        }

        public void rexSetAvatarMorph(string vAvatar, string vMorphName, float vWeight, float vTime) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    World.ForEachScenePresence(delegate(ScenePresence controller)
                    {
                        if (controller.ControllingClient is RexNetwork.RexClientView)
                        {
                            ((RexNetwork.RexClientView)controller.ControllingClient).SendRexAvatarMorph(target.UUID, vMorphName, vWeight, vTime);
                        }
                    });
                    //World.SendRexSetAvatarMorphToAll(target.UUID, vMorphName, vWeight, vTime);
                }
            }
            catch { }
        }

        public void rexPlayMeshAnim(string vPrimId, string vAnimName, float vRate, bool vbLooped, bool vbStopAnim)
        {
            try
            {
                SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimId, 10));
                if (target != null)
                {
                    World.ForEachScenePresence(delegate(ScenePresence controller)
                    {
                        if (controller.ControllingClient is RexNetwork.RexClientView)
                        {
                            ((RexNetwork.RexClientView)controller.ControllingClient).SendRexMeshAnimation(target.UUID, vAnimName, vRate, vbLooped, vbStopAnim);
                        }
                    });
                    //World.SendRexPlayMeshAnimToAll(target.UUID, vAnimName, vRate, vbLooped, vbStopAnim);
                }
            }
            catch { }
        }

        public void rexSetFog(string vAvatar, float vStart, float vEnd, float vR, float vG, float vB) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexFog(vStart, vEnd, vR, vG, vB);
                    }
                }
            }
            catch { }
        }

        public void rexSetWaterHeight(string vAvatar, float vHeight) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexWaterHeight(vHeight);
                    }
                }
            }
            catch { }
        }

        public void rexSetPostProcess(string vAvatar, int vEffectId, bool vbToggle) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexPostProcess(vEffectId, vbToggle);
                    }
                }
            }
            catch { }
        }

        public void rexRttCamera(string vAvatar, int command, string name, string assetID, LSL_Types.Vector3 vPos, LSL_Types.Vector3 vLookAt, int width, int height) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    Vector3 pos = new Vector3((float)vPos.x, (float)vPos.y, (float)vPos.z);
                    Vector3 lookat = new Vector3((float)vLookAt.x, (float)vLookAt.y, (float)vLookAt.z);
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexRttCamera(command, name, new UUID(assetID), pos, lookat, width, height);
                    }
                }
            }
            catch { }
        }

        public void rexRttCameraWorld(string vAvatar, int command, string name, string assetID, LSL_Types.Vector3 vPos, LSL_Types.Vector3 vLookAt, int width, int height) // rex
        {
            try
            {
                Vector3 pos = new Vector3((float)vPos.x, (float)vPos.y, (float)vPos.z);
                Vector3 lookat = new Vector3((float)vLookAt.x, (float)vLookAt.y, (float)vLookAt.z);
                World.ForEachScenePresence(delegate(ScenePresence controller)
                {
                    if (controller.ControllingClient is RexNetwork.RexClientView)
                    {
                        ((RexNetwork.RexClientView)controller.ControllingClient).SendRexRttCamera(command, name, new UUID(assetID), pos, lookat, width, height);
                    }
                });
                //World.SendRexRttCameraToAll(command, name, new UUID(assetID), pos, lookat, width, height);
            }
            catch { }
        }

        public void rexSetViewport(string vAvatar, int command, string name, float vX, float vY, float vWidth, float vHeight) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexViewport(command, name, vX, vY, vWidth, vHeight);
                    }
                }
            }
            catch { }
        }

        public void rexSetAvatarOverrideAddress(string vAvatar, string vAvatarAddress) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView rexClient = (RexNetwork.RexClientView)target.ControllingClient;
                        rexClient.RexAvatarURL = vAvatarAddress;
                        //No need to send appearance to others manually. RexClientView handles that.
                    }
                }
            }
            catch { }
        }

        public void rexToggleWindSound(string vAvatar, bool vbToggle) // rex
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(vAvatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexToggleWindSound(vbToggle);
                    }
                }
            }
            catch { }
        }
        public void rexSetClientSideEffect(string assetId, float vTimeUntilLaunch, float vTimeUntilDeath, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot, float vSpeed)  // rex
        {
            try
            {
                Vector3 pos = new Vector3((float)vPos.x, (float)vPos.y, (float)vPos.z);
                Quaternion rot = new Quaternion((float)vRot.x, (float)vRot.y, (float)vRot.z, (float)vRot.s);
                World.ForEachScenePresence(delegate(ScenePresence controller)
                {
                    if (controller.ControllingClient is RexNetwork.RexClientView)
                    {
                        ((RexNetwork.RexClientView)controller.ControllingClient).SendRexClientSideEffect(assetId, vTimeUntilLaunch, vTimeUntilDeath, pos, rot, vSpeed);
                    }
                });
                //World.SendRexClientSideEffectToAll(new UUID(assetId), vTimeUntilLaunch, vTimeUntilDeath, pos, rot, vSpeed);
            }
            catch { }
        }

        public void rexSetClientSideEffect(string assetName, int assetType, float vTimeUntilLaunch, float vTimeUntilDeath, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot, float vSpeed)  // rex
        {
            throw new NotImplementedException("Could not get asset by name. Use method with uuid instead");
            //try
            //{
            //   UUID tempid = World.AssetCache.ExistsAsset((sbyte)assetType, assetName);
            //   if (tempid != UUID.Zero)
            //   {
            //      rexSetClientSideEffect(tempid.ToString(), vTimeUntilLaunch, vTimeUntilDeath, vPos, vRot, vSpeed);
            //   }
            //}
            //catch { }
        }
        public void rexSetCameraClientSideEffect(string avatar, bool enable, string assetId, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot)  // rex
        {
            try
            {
                Vector3 pos = new Vector3((float)vPos.x, (float)vPos.y, (float)vPos.z);
                Quaternion rot = new Quaternion((float)vRot.x, (float)vRot.y, (float)vRot.z, (float)vRot.s);

                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexCameraClientSideEffect(enable, new UUID(assetId), pos, rot);
                    }
                }
            }
            catch { }
        }

        public void rexSetCameraClientSideEffect(string avatar, bool enable, string assetName, int assetType, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot)  // rex
        {
            throw new NotImplementedException("Could not set camera client side effets. Asset search by name disabled");
            //try
            //{
            //    UUID tempid = World.AssetCache.ExistsAsset((sbyte)assetType, assetName);
            //    if (tempid != UUID.Zero)
            //    {
            //        rexSetCameraClientSideEffect(avatar, enable, tempid.ToString(), vPos, vRot);
            //    }
            //}
            //catch { }
        }


        public string rexRaycast(LSL_Types.Vector3 vPos, LSL_Types.Vector3 vDir, float vLength, string vIgnoreId)
        {
            throw new NotImplementedException("RexRaycast not implemented");
            //uint tempignoreid = 0;

            //if (vIgnoreId.Length > 0)
            //    tempignoreid = System.Convert.ToUInt32(vIgnoreId, 10);

            //return World.RexRaycast(new Vector3((float)vPos.x, (float)vPos.y, (float)vPos.z), new Vector3((float)vDir.x, (float)vDir.y, (float)vDir.z), vLength, tempignoreid);
        }

        public void rexSetAmbientLight(string avatar, LSL_Types.Vector3 lightDirection, LSL_Types.Vector3 lightColour, LSL_Types.Vector3 ambientColour)
        {
            try
            {
                Vector3 lightDir = new Vector3((float)lightDirection.x, (float)lightDirection.y, (float)lightDirection.z);
                Vector3 lightC = new Vector3((float)lightColour.x, (float)lightColour.y, (float)lightColour.z);
                Vector3 ambientC = new Vector3((float)ambientColour.x, (float)ambientColour.y, (float)ambientColour.z);

                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexSetAmbientLight(lightDir, lightC, ambientC);
                    }
                }
            }
            catch { }
        }

        public void rexSetSky(string avatar, int type, string images, float curvature, float tiling)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexSky(type, images, curvature, tiling);
                    }
                }
            }
            catch { }
        }

        public void rexPlayFlashAnimation(string avatar, string assetId, float left, float top, float right, float bottom, float timeToDeath)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexPlayFlashAnimation(new UUID(assetId), left, top, right, bottom, timeToDeath);
                    }
                }
            }
            catch { }
        }

        public void rexPlayFlashAnimation(string avatar, string assetName, int assetType, float left, float top, float right, float bottom, float timeToDeath)
        {
            throw new NotImplementedException("Could not play flash animation. Asset search by name disabled");
            //try
            //{
            //   UUID tempid = World.AssetCache.ExistsAsset((sbyte)assetType, assetName);
            //   if (tempid != UUID.Zero)
            //   {
            //      rexPlayFlashAnimation(avatar, tempid.ToString(), left, top, right, bottom, timeToDeath);
            //   }
            //}
            //catch { }
        }

        public void rexPreloadAssets(string avatar, List<String> vAssetsList)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    AssetBase tempasset = null;
                    Dictionary<UUID, uint> tempassetlist = new Dictionary<UUID, uint>();

                    for (int i = 0; i < vAssetsList.Count; i++)
                    {
                        tempasset = World.AssetCache.GetAsset(new UUID(vAssetsList[i]), false);
                        //tempasset = World.AssetCache.FetchAsset(new UUID(vAssetsList[i]));
                        if (tempasset != null)
                            tempassetlist.Add(tempasset.Metadata.FullID, (uint)tempasset.Metadata.Type);
                    }
                    if (tempassetlist.Count > 0)
                    {
                        if (target.ControllingClient is RexNetwork.RexClientView)
                        {
                            RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                            targetClient.SendRexPreloadAssets(tempassetlist);
                        }
                    }
                }
            }
            catch { }
        }

        public void rexPreloadAvatarAssets(string avatar, List<String> vAssetsList)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (vAssetsList.Count > 0)
                    {
                        if (target.ControllingClient is RexNetwork.RexClientView)
                        {
                            RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                            targetClient.SendRexPreloadAvatarAssets(vAssetsList);
                        }
                    }
                }
            }
            catch { }
        }

        public void rexForceFOV(string avatar, float fov, bool enable)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexForceFOV(fov, enable);
                    }
                }
            }
            catch { }
        }

        public void rexForceCamera(string avatar, int forceMode, float minZoom, float maxZoom)
        {
            try
            {
                ScenePresence target = World.GetScenePresence(new UUID(avatar));
                if (target != null)
                {
                    if (target.ControllingClient is RexNetwork.RexClientView)
                    {
                        RexNetwork.RexClientView targetClient = (RexNetwork.RexClientView)target.ControllingClient;
                        targetClient.SendRexForceCamera(forceMode, minZoom, maxZoom);
                    }
                }
            }
            catch { }
        }

        public void rexAddInitialPreloadAssets(List<String> vAssetsList)
        {
            throw new NotImplementedException("Asset preload not implemented");
            //try
            //{
            //    for (int i = 0; i < vAssetsList.Count; i++)
            //        World.RexAddPreloadAsset(new UUID(vAssetsList[i]));
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("[ScriptEngine]: rexAddInitialPreloadAssets:" + e.ToString());
            //}
        }

        public void rexRemoveInitialPreloadAssets(List<String> vAssetsList)
        {
            throw new NotImplementedException("Asset preload not implemented");
            //try
            //{
            //    for (int i = 0; i < vAssetsList.Count; i++)
            //        World.RexRemovePreloadAsset(new UUID(vAssetsList[i]));
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("[ScriptEngine]: rexRemoveInitialPreloadAssets:" + e.ToString());
            //}
        }

        public string rexGetPrimFreeData(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexData;
            }
            else
                return String.Empty;
        }

        public void rexSetPrimFreeData(string vPrimLocalId, string vData)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexData = vData;
                //RexObjects.RexObjectPart rexobject = (RexObjects.RexObjectPart)target;
                //rexobject.RexData = vData;
            }
            else
            {
                m_log.Warn("[REXSCRIPT]: rexSetPrimFreeData, target prim not found:" + vPrimLocalId);
            }
        }

        public bool rexGetTemporaryPrim(string vPrimLocalId)
        {
            throw new NotImplementedException("rexGetTemporaryPrim not implemented");
            //SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            //if (target != null && target.ParentGroup != null)
            //{
            //    RexObjects.RexObjectPart rexobject = (RexObjects.RexObjectPart)target;
            //    return rexobject.ParentGroup.TemporaryPrim;
            //}
            //else
            //    return false;
        }

        public void rexSetTemporaryPrim(string vPrimLocalId, bool vbData)
        {
            throw new NotImplementedException("rexSetTemporaryPrim not implemented");
            //SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            //if (target != null && target.ParentGroup != null)
            //{
            //    RexObjects.RexObjectPart rexobject = (RexObjects.RexObjectPart)target;
            //    rexobject.ParentGroup.TemporaryPrim = vbData;
            //}
            //else
            //{
            //    m_log.Warn("[REXSCRIPT]: rexSetTemporaryPrim, target prim not found:" + vPrimLocalId);
            //}
        }

        public void rexPlayClientSound(string vAvatar, string sound, double volume)
        {
            try
            {
                ScenePresence targetavatar = World.GetScenePresence(new UUID(vAvatar));
                if (targetavatar == null)
                {
                    m_log.Warn("[REXSCRIPT]: rexPlayClientSound, target avatar not found:" + vAvatar);
                    return;
                }
                UUID soundID = UUID.Zero;
                if (!UUID.TryParse(sound, out soundID))
                {
                    ;
                    //soundID = World.AssetCache.ExistsAsset(1, sound);
                }
                if (soundID != UUID.Zero)
                    targetavatar.ControllingClient.SendPlayAttachedSound(soundID, targetavatar.ControllingClient.AgentId, targetavatar.ControllingClient.AgentId, (float)volume, 0);
                else
                {
                    m_log.Warn("[REXSCRIPT]: rexPlayClientSound, sound not found:" + sound);
                }
            }
            catch (Exception e) { m_log.Error("[REXSCRIPT]: Could not play sound file.", e); }
        }

        #region RexPrimdata variables

        public int GetRexDrawType(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return (int)rop.RexDrawType;
            }
            else
                return 0;
        }

        public void SetRexDrawType(string vPrimLocalId, int vDrawType)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexDrawType = (byte)vDrawType;
            }
            else
            {
                m_log.Warn("[REXSCRIPT]: SetRexDrawType, target prim not found:" + vPrimLocalId);
            }
        }

        public bool GetRexIsVisible(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexIsVisible;
            }
            else
                return false;
        }

        public void SetRexIsVisible(string vPrimLocalId, bool vbIsVisible)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexIsVisible = vbIsVisible;
            }
            else
            {
                m_log.Warn("[REXSCRIPT]: SetRexIsVisible, target prim not found:" + vPrimLocalId);
            }
        }

        public bool GetRexCastShadows(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexCastShadows;
            }
            else
                return false;
        }

        public void SetRexCastShadows(string vPrimLocalId, bool vbCastShadows)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexCastShadows = vbCastShadows;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexCastShadows, target prim not found:" + vPrimLocalId);
        }

        public bool GetRexLightCreatesShadows(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexLightCreatesShadows;
            }
            else
                return false;
        }

        public void SetRexLightCreatesShadows(string vPrimLocalId, bool vbLightCreates)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexLightCreatesShadows = vbLightCreates;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexLightCreatesShadows, target prim not found:" + vPrimLocalId);
        }

        public bool GetRexDescriptionTexture(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexDescriptionTexture;
            }
            else
                return false;
        }

        public void SetRexDescriptionTexture(string vPrimLocalId, bool vbDescTex)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexDescriptionTexture = vbDescTex;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexDescriptionTexture, target prim not found:" + vPrimLocalId);
        }

        public bool GetRexScaleToPrim(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexScaleToPrim;
            }
            else
                return false;
        }

        public void SetRexScaleToPrim(string vPrimLocalId, bool vbScale)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexScaleToPrim = vbScale;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexScaleToPrim, target prim not found:" + vPrimLocalId);
        }

        public float GetRexDrawDistance(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexDrawDistance;
            }
            else
                return 0;
        }

        public void SetRexDrawDistance(string vPrimLocalId, float vDist)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexDrawDistance = vDist;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexDrawDistance, target prim not found:" + vPrimLocalId);
        }

        public float GetRexLOD(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexLOD;
            }
            else
                return 0;
        }

        public void SetRexLOD(string vPrimLocalId, float vLod)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexLOD = vLod;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexLOD, target prim not found:" + vPrimLocalId);
        }

        public string GetRexMeshUUID(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexMeshUUID.ToString();
            }
            else
                return String.Empty;
        }

        public void SetRexMeshUUID(string vPrimLocalId, string vsUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexMeshUUID = new UUID(vsUUID);
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexMeshUUID, target prim not found:" + vPrimLocalId);
        }

        public void SetRexMeshByName(string vPrimLocalId, string vsName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SetRexMeshUUID(vPrimLocalId, World.AssetCache.ExistsAsset(43, vsName).ToString());
        }

        public string GetRexCollisionMeshUUID(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexCollisionMeshUUID.ToString();
            }
            else
                return String.Empty;
        }

        public void SetRexCollisionMeshUUID(string vPrimLocalId, string vsUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexCollisionMeshUUID = new UUID(vsUUID);
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexCollisionMeshUUID, target prim not found:" + vPrimLocalId);
        }

        public void SetRexCollisionMeshByName(string vPrimLocalId, string vsName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SetRexCollisionMeshUUID(vPrimLocalId, World.AssetCache.ExistsAsset(43, vsName).ToString());
        }

        public string GetRexParticleScriptUUID(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexParticleScriptUUID.ToString();
            }
            else
                return String.Empty;
        }

        public void SetRexParticleScriptUUID(string vPrimLocalId, string vsUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexParticleScriptUUID = new UUID(vsUUID);
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexParticleScriptUUID, target prim not found:" + vPrimLocalId);
        }

        public void SetRexParticleScriptByName(string vPrimLocalId, string vsName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SetRexParticleScriptUUID(vPrimLocalId, World.AssetCache.ExistsAsset(47, vsName).ToString());
        }

        public string GetRexAnimationPackageUUID(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexAnimationPackageUUID.ToString();
            }
            else
                return String.Empty;
        }

        public void SetRexAnimationPackageUUID(string vPrimLocalId, string vsUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexAnimationPackageUUID = new UUID(vsUUID);
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexAnimationPackageUUID, target prim not found:" + vPrimLocalId);
        }

        public void SetRexAnimationPackageByName(string vPrimLocalId, string vsName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SetRexAnimationPackageUUID(vPrimLocalId, World.AssetCache.ExistsAsset(44, vsName).ToString());
        }

        public string GetRexAnimationName(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexAnimationName;
            }
            else
                return String.Empty;
        }

        public void SetRexAnimationName(string vPrimLocalId, string vName)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexAnimationName = vName;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexAnimationName, target prim not found:" + vPrimLocalId);
        }

        public float GetRexAnimationRate(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexAnimationRate;
            }
            else
                return 0;
        }

        public void SetRexAnimationRate(string vPrimLocalId, float vAnimRate)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexAnimationRate = vAnimRate;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexAnimationRate, target prim not found:" + vPrimLocalId);
        }

        public string RexGetMaterial(string vPrimLocalId, int vIndex)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                if (rop.RexMaterials.ContainsKey((uint)vIndex))
                    return rop.RexMaterials[(uint)vIndex].ToString();
            }
            return String.Empty;
        }

        public int RexGetMaterialCount(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexMaterials.Count;
            }
            else
                return 0;
        }

        public void RexSetMaterialUUID(string vPrimLocalId, int vIndex, string vsMatUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexMaterials.AddMaterial((uint)vIndex, new UUID(vsMatUUID));
            }
            else
                m_log.Warn("[REXSCRIPT]: RexSetMaterialUUID, target prim not found:" + vPrimLocalId);
        }

        public void RexSetMaterialByName(string vPrimLocalId, int vIndex, string vsMatName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            //if (target != null)
            //{
            //    if (target is RexObjects.RexObjectPart)
            //    {
            //        UUID tempmatid = World.AssetCache.ExistsAsset(0, vsMatName);
            //        if (tempmatid == UUID.Zero)
            //            tempmatid = World.AssetCache.ExistsAsset(45, vsMatName);

            //        RexObjects.RexObjectPart rexTarget = (RexObjects.RexObjectPart)target;
            //        rexTarget.RexMaterials.AddMaterial((uint)vIndex, tempmatid);
            //    }
            //}
            //else
            //    Console.WriteLine("[ScriptEngine]: RexSetMaterialByName, target prim not found:" + vPrimLocalId);
        }

        public string GetRexClassName(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexClassName;
            }
            else
                return String.Empty;
        }

        public void SetRexClassName(string vPrimLocalId, string vsClassName)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexClassName = vsClassName;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexClassName, target prim not found:" + vPrimLocalId);
        }

        public string GetRexSoundUUID(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexSoundUUID.ToString();

            }
            return String.Empty;
        }

        public void SetRexSoundUUID(string vPrimLocalId, string vsUUID)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexSoundUUID = new UUID(vsUUID);
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexSoundUUID, target prim not found:" + vPrimLocalId);
        }

        public void SetRexSoundByName(string vPrimLocalId, string vsName)
        {
            throw new NotImplementedException("Could not get asset by name");
            //SetRexSoundUUID(vPrimLocalId, World.AssetCache.ExistsAsset(1, vsName).ToString());
        }

        public float GetRexSoundVolume(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {

                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexSoundVolume;

            }
            return 0;
        }

        public void SetRexSoundVolume(string vPrimLocalId, float vVolume)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {

                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexSoundVolume = vVolume;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexSoundVolume, target prim not found:" + vPrimLocalId);
        }

        public float GetRexSoundRadius(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexSoundRadius;

            }
            return 0;
        }

        public void SetRexSoundRadius(string vPrimLocalId, float vRadius)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexSoundRadius = vRadius;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexSoundRadius, target prim not found:" + vPrimLocalId);
        }

        public int GetRexSelectPriority(string vPrimLocalId)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {
                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                return rop.RexSelectPriority;

            }
            return 0;
        }

        public void SetRexSelectPriority(string vPrimLocalId, int vValue)
        {
            SceneObjectPart target = World.GetSceneObjectPart(System.Convert.ToUInt32(vPrimLocalId, 10));
            if (target != null)
            {

                RexObjectProperties rop = m_rexObjects.GetObject(target.UUID);
                rop.RexSelectPriority = vValue;
            }
            else
                m_log.Warn("[REXSCRIPT]: SetRexSelectPriority, target prim not found:" + vPrimLocalId);
        }


        #endregion
    }
}
