# rxactor.py
# Parent class for all actors in the world.
#print "rxactor.................................."

import sys
import clr
clr.AddReferenceToFile("ModularRex.dll")

import rxlslobject
import rxworld
import rxtimer

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3
Quaternion = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Quaternion

class Actor(rxlslobject.LSLObject):

    def __init__(self, vId):
        #super(Actor,self).__init__()
        self.MyWorld = None
        self.Id = str(vId)

        self.MyTag = ""
        self.MyEvent = ""
        
        self.MyTimerCount = 0
        self.bTimerLoop = False

    #def __del__(self):
    #    #print "DELETING ACTOR!"
    #    #super(self.__class__, self).__del__()

    @staticmethod
    def GetScriptClassName():
        return "rxactor.Actor"

    def GetId(self):
        return str(self.Id)
    def GetPrimLocalIdFromUUID(self,vUUID):
        return self.MyWorld.CS.GetPrimLocalIdFromUUID(vUUID)

    # Send python command to client
    def CommandToClient(self,vAgentId,vUnit,vCommand,vCmdParams):
        self.MyWorld.CS.CommandToClient(vAgentId,vUnit,vCommand,vCmdParams)

    # Velocity
    def SetVelocity(self,vVelocity):
        return self.MyWorld.CS.SetVelocity(self.Id,vVelocity)
    Velocity = property(fget=lambda self: self.llGetVel(),fset=lambda self, v: self.SetVelocity(v))

    # Physics
    def GetPhysics(self):
        return self.MyWorld.CS.GetPhysics(self.Id)
    def SetPhysics(self,vbPhysics):
        return self.MyWorld.CS.SetPhysics(self.Id,vbPhysics)
    Physics = property(fget=lambda self: self.GetPhysics(),fset=lambda self, v: self.SetPhysics(v))

    #Mass
    def SetMass(self,vMass):
        return self.MyWorld.CS.SetMass(self.Id,vMass)
    Mass = property(fget=lambda self: self.llGetMass(),fset=lambda self, v: self.SetMass(v))


    def GetUsePrimVolumeCollision(self):
        return self.MyWorld.CS.GetUsePrimVolumeCollision(self.Id)
    def SetUsePrimVolumeCollision(self,vUsePrimVolumeCol):
        self.MyWorld.CS.SetUsePrimVolumeCollision(self.Id,vUsePrimVolumeCol)

    def SendGeneralAlertAll(self,vString):
        self.MyWorld.CS.SendGeneralAlertAll(self.Id,vString)
    def SendAlertToAvatar(self,vAgentId,vString,vbModal):
        self.MyWorld.CS.SendAlertToAvatar(self.Id,vAgentId,vString,vbModal)
    def PlayClientSound(self,vAgentId,vSound,vVolume): # vSound can be lluuid or name
        self.MyWorld.CS.rexPlayClientSound(vAgentId,vSound,vVolume)

    def GetRadiusActors(self,vRadius):
        return self.MyWorld.CS.GetRadiusActors(self.Id,vRadius)
    def GetRadiusAvatars(self,vRadius):
        return self.MyWorld.CS.GetRadiusAvatars(self.Id,vRadius)

    def EnableTick(self):
        self.MyWorld.MyEventManager.EnableTickForActor(self)
    def DisableTick(self):
        self.MyWorld.MyEventManager.DisableTickForActor(self)

    def SetTimer(self,vTime,vbLoop):
        self.MyWorld.MyEventManager.SetTimerForActor(self,vTime,vbLoop)
        
    def CreateRexTimer(self,vTime,vTimesActivated):
        return rxtimer.RexTimer(vTime,vTimesActivated)
        
    def SpawnActor(self,vLoc,vIndex,vbTemprorary,vPyClass):
        return self.MyWorld.CS.SpawnActor(vLoc,vIndex,vbTemprorary,vPyClass)
    def DestroyActor(self):
        return self.MyWorld.CS.DestroyActor(self.Id)

    def rexRaycast(self,vStartPos,vDir,vLength,vIgnoreId):
        objid = self.MyWorld.CS.rexRaycast(vStartPos,vDir,vLength,vIgnoreId)
        if(objid == 0) or (not self.MyWorld.AllActors.has_key(objid)):
            return None
        else:
            return self.MyWorld.AllActors[objid]

    def rexPlayMeshAnimation(self,vAnimName,vRate,vbLooped, vbStopAnim):
        return self.MyWorld.CS.rexPlayMeshAnim(self.Id,vAnimName,vRate,vbLooped,vbStopAnim)

    def rexSetClientSideEffectByUUID(self,assetId,vTimeUntilLaunch,vTimeUntilDeath,vPos,vRot,vSpeed):
        self.MyWorld.CS.rexSetClientSideEffect(assetId,vTimeUntilLaunch,vTimeUntilDeath,vPos,vRot,vSpeed)

    def rexSetClientSideEffect(self,assetName,assetType,vTimeUntilLaunch,vTimeUntilDeath,vPos,vRot,vSpeed):
        self.MyWorld.CS.rexSetClientSideEffect(assetName,assetType,vTimeUntilLaunch,vTimeUntilDeath,vPos,vRot,vSpeed)

#    def rexSetTextureMediaURL(self, url):
#        return self.MyWorld.CS.rexSetTextureMediaURL(url)

    def rexSetTextureMediaURL(self, url, refreshRate=None):
        self.MyWorld.CS.SetScriptRunner(self.Id)
        if refreshRate is None:
            return self.MyWorld.CS.rexSetTextureMediaURL(url, 0)
        else: 
            return self.MyWorld.CS.rexSetTextureMediaURL(url, refreshRate)

    def rexAddInitialPreloadAssets(self,vAssetList):
        self.MyWorld.CS.rexAddInitialPreloadAssets(vAssetList)
    def rexRemoveInitialPreloadAssets(self,vAssetList):
        self.MyWorld.CS.rexRemoveInitialPreloadAssets(vAssetList)


    # Scale
    Scale = property(fget=lambda self: self.llGetScale(),fset=lambda self, v: self.llSetScale(v))

    def GetTime(self):
        return self.MyWorld.MyEventManager.CurrTime

    # Rexprimdata variables
    def GetRexDrawType(self):
        return self.MyWorld.CS.GetRexDrawType(self.Id)
    def SetRexDrawType(self,vDrawType):
        self.MyWorld.CS.SetRexDrawType(self.Id,vDrawType)
    def GetRexIsVisible(self):
        return self.MyWorld.CS.GetRexIsVisible(self.Id)
    def SetRexIsVisible(self,vbIsVisible):
        self.MyWorld.CS.SetRexIsVisible(self.Id,vbIsVisible)
    def GetRexCastShadows(self):
        return self.MyWorld.CS.GetRexCastShadows(self.Id)
    def SetRexCastShadows(self,vbCastShadows):
        self.MyWorld.CS.SetRexCastShadows(self.Id,vbCastShadows)
    def GetRexLightCreatesShadows(self):
        return self.MyWorld.CS.GetRexLightCreatesShadows(self.Id)
    def SetRexLightCreatesShadows(self, vbLightCreates):
        self.MyWorld.CS.SetRexLightCreatesShadows(self.Id,vbLightCreates)
    def GetRexDescriptionTexture(self):
        return self.MyWorld.CS.GetRexDescriptionTexture(self.Id)
    def SetRexDescriptionTexture(self, vbDescTex):
        self.MyWorld.CS.SetRexDescriptionTexture(self.Id,vbDescTex)
    def GetRexScaleToPrim(self):
        return self.MyWorld.CS.GetRexScaleToPrim(self.Id)
    def SetRexScaleToPrim(self, vbScale):
        self.MyWorld.CS.SetRexScaleToPrim(self.Id,vbScale)

    def GetRexDrawDistance(self):
        return self.MyWorld.CS.GetRexDrawDistance(self.Id)
    def SetRexDrawDistance(self, vDist):
        self.MyWorld.CS.SetRexDrawDistance(self.Id,vDist)
    def GetRexLOD(self):
        return self.MyWorld.CS.GetRexLOD(self.Id)
    def SetRexLOD(self, vLod):
        self.MyWorld.CS.SetRexLOD(self.Id,vLod)

    def GetRexMeshUUID(self):
        return self.MyWorld.CS.GetRexMeshUUID(self.Id)
    def SetRexMeshUUID(self, vsLLUUID):
        self.MyWorld.CS.SetRexMeshUUID(self.Id,vsLLUUID)
    def SetRexMeshByName(self, vsName):
        self.MyWorld.CS.SetRexMeshByName(self.Id,vsName)
    def GetRexCollisionMeshUUID(self):
        return self.MyWorld.CS.GetRexCollisionMeshUUID(self.Id)
    def SetRexCollisionMeshUUID(self, vsLLUUID):
        self.MyWorld.CS.SetRexCollisionMeshUUID(self.Id,vsLLUUID)
    def SetRexCollisionMeshByName(self,vsName):
        self.MyWorld.CS.SetRexCollisionMeshByName(self.Id,vsName)

    def GetRexParticleScriptUUID(self):
        return self.MyWorld.CS.GetRexParticleScriptUUID(self.Id)
    def SetRexParticleScriptUUID(self, vsLLUUID):
        self.MyWorld.CS.SetRexParticleScriptUUID(self.Id,vsLLUUID)
    def SetRexParticleScriptByName(self, vsName):
        self.MyWorld.CS.SetRexParticleScriptByName(self.Id,vsName)

    def GetRexAnimationPackageUUID(self):
        return self.MyWorld.CS.GetRexAnimationPackageUUID(self.Id)
    def SetRexAnimationPackageUUID(self,vsLLUUID):
        self.MyWorld.CS.SetRexAnimationPackageUUID(self.Id,vsLLUUID)
    def SetRexAnimationPackageByName(self, vsName):
        self.MyWorld.CS.SetRexAnimationPackageByName(self.Id,vsName)
    def GetRexAnimationName(self):
        return self.MyWorld.CS.GetRexAnimationName(self.Id)
    def SetRexAnimationName(self,vsName):
        self.MyWorld.CS.SetRexAnimationName(self.Id,vsName)
    def GetRexAnimationRate(self):
        return self.MyWorld.CS.GetRexAnimationRate(self.Id)
    def SetRexAnimationRate(self,vAnimRate):
        self.MyWorld.CS.SetRexAnimationRate(self.Id,vAnimRate)

    def RexGetMaterial(self,vIndex):
        return self.MyWorld.CS.RexGetMaterial(self.Id,vIndex)
    def RexGetMaterialCount(self):
        return self.MyWorld.CS.RexGetMaterialCount(self.Id)
    def RexSetMaterialUUID(self,vIndex,vsMatLLUUID):
        self.MyWorld.CS.RexSetMaterialUUID(self.Id,vIndex,vsMatLLUUID)
    def RexSetMaterialByName(self,vIndex,vsMatName):
        self.MyWorld.CS.RexSetMaterialByName(self.Id,vIndex,vsMatName)

    def GetRexClassName(self):
        return self.MyWorld.CS.GetRexClassName(self.Id)
    def SetRexClassName(self,vsName):
        self.MyWorld.CS.SetRexClassName(self.Id,vsName)

    def GetRexSoundUUID(self):
        return self.MyWorld.CS.GetRexSoundUUID(self.Id)
    def SetRexSoundUUID(self,vsLLUUID):
        self.MyWorld.CS.SetRexSoundUUID(self.Id,vsLLUUID)
    def SetRexSoundByName(self,vsName):
        self.MyWorld.CS.SetRexSoundByName(self.Id,vsName)
    def GetRexSoundVolume(self):
        return self.MyWorld.CS.GetRexSoundVolume(self.Id)
    def SetRexSoundVolume(self,vVolume):
        self.MyWorld.CS.SetRexSoundVolume(self.Id,vVolume)
    def GetRexSoundRadius(self):
        return self.MyWorld.CS.GetRexSoundRadius(self.Id)
    def SetRexSoundRadius(self,vRadius):
        self.MyWorld.CS.SetRexSoundRadius(self.Id,vRadius)

    def rexGetPrimFreeData(self):
        return self.MyWorld.CS.rexGetPrimFreeData(self.Id)
    def rexSetPrimFreeData(self,vData):
        self.MyWorld.CS.rexSetPrimFreeData(self.Id,vData)
    PrimFreeData = property(fget=lambda self: self.rexGetPrimFreeData(),fset=lambda self, v: self.rexSetPrimFreeData(v))

    def GetRexSelectPriority(self):
        return self.MyWorld.CS.GetRexSelectPriority(self.Id)
    def SetRexSelectPriority(self,vValue):
        self.MyWorld.CS.SetRexSelectPriority(self.Id,vValue)

    def GetRexTemporaryPrim(self):
        return self.MyWorld.CS.rexGetTemporaryPrim(self.Id)
    def SetRexTemporaryPrim(self,vValue):
        self.MyWorld.CS.rexSetTemporaryPrim(self.Id,vValue)
    def AttachObjectToAvatar(self, avatarId, attachmentPoint, pos=Vector3(0,0,0), rot=Quaternion(0,0,0,1), silent=False):
        self.MyWorld.CS.rexAttachObjectToAvatar(self.Id, avatarId, attachmentPoint, pos, rot, silent)

    # EC-Attribute access
    def rexGetECAttributes(self, typeName, name = ""):
        return self.MyWorld.CS.rexGetECAttributes(self.Id, typeName, name)
    def rexSetECAttributes(self, attributes, typeName, name = ""):
        return self.MyWorld.CS.rexSetECAttributes(self.Id, attributes, typeName, name)

    # Deprecated functions
    def SetMesh(self,vMeshName):
        print "SetMesh deprecated, use SetRexMeshByName"
    def SetMeshByLLUUID(self,vMeshLLUUID):
        print "SetMeshByLLUUID deprecated, use SetRexMeshUUID"
    def SetMaterial(self,vIndex,vName):
        print "SetMaterial deprecated, use RexSetMaterialByName"
    def SetParticleScript(self,vParticleScriptName):
        print "SetParticleScript deprecated, use SetRexParticleScriptByName"

    # Events
    def EventPreCreated(self):
        pass

    def EventCreated(self):
        pass
        
    def EventDestroyed(self):
        pass

    def EventTouch(self,vAvatar):
        pass

    def EventTick(self,vDeltaTime):
        pass

    def EventTimer(self):
        pass

    def EventTrigger(self,vOther):
        pass

    def EventPrimVolumeCollision(self,vOther):
        pass


    # Trigger event
    def TriggerEvent(self,vEventStr,vOther):
        if len(vEventStr) == 0:
            print "TriggerEvent, no event string defined"
            return
        
        for iid, iactor in self.MyWorld.AllActors.iteritems():
            if iactor.MyTag == vEventStr:
                iactor.EventTrigger(vOther)

    def PrintActorList(self):
        print "Printing actor list..."
        print "Length is ",len(self.MyWorld.AllActors)
        for iid, iactor in self.MyWorld.AllActors.iteritems():
            print iid,iactor.Id




    #def GetFreezed(self):
    #    return self.MyWorld.CS.GetFreezed(self.Id)
    #def SetFreezed(self,vbFreeze):
    #    self.MyWorld.CS.SetFreezed(self.Id,vbFreeze)

    # PhysicsMode
    #def GetPhysicsMode(self):
    #    return self.MyWorld.CS.GetPhysicsMode(self.Id)
    #def SetPhysicsMode(self,vPhysicsMode):
    #    return self.MyWorld.CS.SetPhysicsMode(self.Id,vPhysicsMode)
    #PhysicsMode = property(fget=lambda self: self.GetPhysicsMode(),fset=lambda self, v: self.SetPhysicsMode(v))

    # Gravity
    #def GetUseGravity(self):
    #    return self.MyWorld.CS.GetUseGravity(self.Id)
    #def SetUseGravity(self,vbGravity):
    #    return self.MyWorld.CS.SetUseGravity(self.Id,vbGravity)
    #Gravity = property(fget=lambda self: self.GetUseGravity(),fset=lambda self, v: self.SetUseGravity(v))

    #def SetLocationFast(self,vLocation):
    #    self.MyWorld.CS.SetLocationFast(self.Id,vLocation)
