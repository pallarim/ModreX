// rex
using System;
using System.Collections.Generic;
using System.Text;
using OpenSim.Region.ScriptEngine.Shared;

namespace ModularRex.RexParts
{
    public interface Rex_BuiltIn_Commands_Interface
    {
        int rexSetTextureMediaURL(string url);
        
        void rexIKSetLimbTarget(string vAvatar, int vLimbId, LSL_Types.Vector3 vDest, float vTimeToTarget, float vStayTime, 
            float vConstraintAngle, string vStartAnim, string vTargetAnim, string vEndAnim);
            
        void rexPlayAvatarAnim(string vAvatar, string vAnimName, float vRate, float vFadeIn, float vFadeOut, int nRepeats, bool vbStopAnim);
        void rexPlayMeshAnim(string vPrimId, string vAnimName, float vRate, bool vbLooped, bool vbStopAnim);
        void rexSetAvatarMorph(string vAvatar, string vMorphName, float vWeight, float vTime);
        void rexSetFog(string vAvatar, float vStart, float vEnd, float vR, float vG, float vB);
        void rexSetWaterHeight(string vAvatar, float vHeight);
        void rexSetPostProcess(string vAvatar, int vEffectId, bool vbToggle);
        void rexSetAvatarOverrideAddress(string vAvatar, string vAvatarAddress);
        
        void rexRttCamera(string vAvatar, int command, string name, string assetID, LSL_Types.Vector3 vPos, LSL_Types.Vector3 vLookAt, int width, int height);
        void rexRttCameraWorld(string vAvatar, int command, string name, string assetID, LSL_Types.Vector3 vPos, LSL_Types.Vector3 vLookAt, int width, int height);
        void rexSetViewport(string vAvatar, int command, string name, float vX, float vY, float vWidth, float vHeight);
        void rexToggleWindSound(string vAvatar, bool vbToggle);
        void rexSetClientSideEffect(string assetId, float vTimeUntilLaunch, float vTimeUntilDeath, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot, float vSpeed);
        void rexSetClientSideEffect(string assetName, int assetType, float vTimeUntilLaunch, float vTimeUntilDeath, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot, float vSpeed);
        void rexSetCameraClientSideEffect(string avatar, bool enable, string assetId, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot);
        void rexSetCameraClientSideEffect(string avatar, bool enable, string assetName, int assetType, LSL_Types.Vector3 vPos, LSL_Types.Quaternion vRot);
        void rexSetAmbientLight(string avatar, LSL_Types.Vector3 lightDirection, LSL_Types.Vector3 lightColour, LSL_Types.Vector3 ambientColour);
        void rexSetSky(string avatar, int type, string images, float curvature, float tiling);
        void rexPlayFlashAnimation(string avatar, string assetId, float left, float top, float right, float bottom, float timeToDeath);
        void rexPlayFlashAnimation(string avatar, string assetName, int assetType, float left, float top, float right, float bottom, float timeToDeath);
        void rexPreloadAssets(string avatar, List<String> vAssetsList);
        void rexPreloadAvatarAssets(string avatar, List<String> vAssetsList);
        void rexAddInitialPreloadAssets(List<String> vAssetsList);
        void rexRemoveInitialPreloadAssets(List<String> vAssetsList);
        bool rexGetTemporaryPrim(string vPrimLocalId);
        void rexSetTemporaryPrim(string vPrimLocalId, bool vbData);
        void rexForceFOV(string avatar, float fov, bool enable);
        void rexForceCamera(string avatar, int forceMode, float minZoom, float maxZoom);
        void rexPlayClientSound(string vAvatar, string sound, double volume);
        
        string rexRaycast(LSL_Types.Vector3 vPos, LSL_Types.Vector3 vDir, float vLength, string vIgnoreId);

        // new primdata variables
        int GetRexDrawType(string vPrimLocalId);
        void SetRexDrawType(string vPrimLocalId,int vDrawType);
        bool GetRexIsVisible(string vPrimLocalId);
        void SetRexIsVisible(string vPrimLocalId, bool vbIsVisible);
        bool GetRexCastShadows(string vPrimLocalId);
        void SetRexCastShadows(string vPrimLocalId, bool vbCastShadows);
        bool GetRexLightCreatesShadows(string vPrimLocalId);
        void SetRexLightCreatesShadows(string vPrimLocalId, bool vbLightCreates);      
        bool GetRexDescriptionTexture(string vPrimLocalId);
        void SetRexDescriptionTexture(string vPrimLocalId, bool vbDescTex);
        bool GetRexScaleToPrim(string vPrimLocalId);      
        void SetRexScaleToPrim(string vPrimLocalId, bool vbScale);
        float GetRexDrawDistance(string vPrimLocalId);
        void SetRexDrawDistance(string vPrimLocalId, float vDist);
        float GetRexLOD(string vPrimLocalId);
        void SetRexLOD(string vPrimLocalId, float vLod);
        string GetRexMeshUUID(string vPrimLocalId);
        void SetRexMeshUUID(string vPrimLocalId, string vsUUID);
        void SetRexMeshByName(string vPrimLocalId, string vsName);
        string GetRexCollisionMeshUUID(string vPrimLocalId);
        void SetRexCollisionMeshUUID(string vPrimLocalId, string vsUUID);
        void SetRexCollisionMeshByName(string vPrimLocalId, string vsName);
        string GetRexParticleScriptUUID(string vPrimLocalId);
        void SetRexParticleScriptUUID(string vPrimLocalId, string vsUUID);
        void SetRexParticleScriptByName(string vPrimLocalId, string vsName);
        string GetRexAnimationPackageUUID(string vPrimLocalId);
        void SetRexAnimationPackageUUID(string vPrimLocalId, string vsUUID);
        void SetRexAnimationPackageByName(string vPrimLocalId, string vsName);
        string GetRexAnimationName(string vPrimLocalId);
        void SetRexAnimationName(string vPrimLocalId, string vName);
        float GetRexAnimationRate(string vPrimLocalId);
        void SetRexAnimationRate(string vPrimLocalId, float vAnimRate);
        string RexGetMaterial(string vPrimLocalId, int vIndex);
        int RexGetMaterialCount(string vPrimLocalId);
        void RexSetMaterialUUID(string vPrimLocalId, int vIndex, string vsMatUUID);
        void RexSetMaterialByName(string vPrimLocalId, int vIndex, string vsMatName);
        string GetRexClassName(string vPrimLocalId);
        void SetRexClassName(string vPrimLocalId, string vsClassName);
        string GetRexSoundUUID(string vPrimLocalId);
        void SetRexSoundUUID(string vPrimLocalId, string vsUUID);
        void SetRexSoundByName(string vPrimLocalId, string vsName);
        float GetRexSoundVolume(string vPrimLocalId);
        void SetRexSoundVolume(string vPrimLocalId, float vVolume);
        float GetRexSoundRadius(string vPrimLocalId);
        void SetRexSoundRadius(string vPrimLocalId, float vRadius);
        string rexGetPrimFreeData(string vPrimLocalId);
        void rexSetPrimFreeData(string vPrimLocalId, string vData);
        int GetRexSelectPriority(string vPrimLocalId);
        void SetRexSelectPriority(string vPrimLocalId, int vValue);
    }
}
