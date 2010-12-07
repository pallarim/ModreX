# rxavatar.py
# Note:
#    Avatar inherits the rxlslobject but NOT all lsl functions from
#    that don't work. Just the ones which are overridden here work.
#    - Tuco

#print "rxavatar.................................."

import sys
import rxactor

class Avatar(rxactor.Actor):

    @staticmethod
    def GetScriptClassName():
        return "rxavatar.Avatar"
    
    def EventCreated(self):
        super(Avatar,self).EventCreated()
        #print "Avatar EventCreated",self.Id
        pass

    def EventDestroyed(self):
        super(Avatar,self).EventDestroyed()
        #print "Avatar EventDestroyed",self.Id
        pass

    def AddGenericMessageHandler(self, method, handler):
        self.MyWorld.CS.AddUserGenericPacketHandler(self.AgentId, method, handler)

    def GetFullName(self):
        return self.MyWorld.CS.SPGetFullName(self.AgentId)
    def GetFirstName(self):
        return self.MyWorld.CS.SPGetFirstName(self.AgentId)
    def GetLastName(self):
        return self.MyWorld.CS.SPGetLastName(self.AgentId)
    def DoLocalTeleport(self,vLocation):
        self.MyWorld.CS.SPDoLocalTeleport(self.AgentId,vLocation)
        
    def llGetPos(self):
        return self.MyWorld.CS.SPGetPos(self.AgentId)
    def llSetPos(self,pos):
        self.MyWorld.CS.SPDoLocalTeleport(self.AgentId,pos)
    def GetRadiusActors(self,vRadius):
        return self.MyWorld.CS.GetRadiusActors(self.AgentId,vRadius)
    def GetRadiusAvatars(self,vRadius):
        return self.MyWorld.CS.GetRadiusAvatars(self.AgentId,vRadius)
    def llGetRot(self):
        return self.MyWorld.CS.SPGetRot(self.AgentId)
    def llSetRot(self,rot):
        return self.MyWorld.CS.SPSetRot(self.AgentId,rot,False)
    def SetRelativeRot(self,rot):
        return self.MyWorld.CS.SPSetRot(self.AgentId,rot,True)
    
    def GetMovementModifier(self):
        return self.MyWorld.CS.SPGetMovementModifier(self.AgentId)
    def SetMovementModifier(self,vSpeedMod):
        self.MyWorld.CS.SPSetMovementModifier(self.AgentId,vSpeedMod)
    def GetVertMovementModifier(self):
        return self.MyWorld.CS.SPGetVertMovementModifier(self.AgentId)
    def SetVertMovementModifier(self,vSpeedMod):
        self.MyWorld.CS.SPSetVertMovementModifier(self.AgentId,vSpeedMod)

    def GetWalkDisabled(self):
        return self.MyWorld.CS.SPGetWalkDisabled(self.AgentId)
    def SetWalkDisabled(self,vValue):
        self.MyWorld.CS.SPSetWalkDisabled(self.AgentId,vValue)
    def GetFlyDisabled(self):
        return self.MyWorld.CS.SPGetFlyDisabled(self.AgentId)
    def SetFlyDisabled(self,vValue):
        self.MyWorld.CS.SPSetFlyDisabled(self.AgentId,vValue)
    def GetSitDisabled(self):
        return self.MyWorld.CS.SPGetSitDisabled(self.AgentId)
    def SetSitDisabled(self,vValue):
        self.MyWorld.CS.SPSetSitDisabled(self.AgentId,vValue)

    def IsHuman(self):
        return True
    def IsBot(self):
        return False

    # Hud functions
    def ShowInventoryMessage(self,vMessage):
        self.CommandToClient(self.AgentId,'hud','ShowInventoryMessage("'+vMessage+'")','')
    def ShowInventoryMessageAdv(self,vMessage,vTime,vR,vG,vB,vA):
        self.CommandToClient(self.AgentId,'hud','ShowInvMessageAdv("'+str(vMessage)+'",'+str(vTime)+','+str(vR)+','+str(vG)+','+str(vB)+','+str(vA)+')','')
    def ShowScrollMessage(self,vMessage,vTime):
        self.CommandToClient(self.AgentId,'hud','ShowScrollMessage("'+vMessage+'",'+str(vTime)+')','')
    def ShowTutorialBox(self,vMessage,vTime):
        self.CommandToClient(self.AgentId,'hud','ShowTutorialBox("'+vMessage+'",'+str(vTime)+')','')
    def DoFadeInOut(self,vIn,vBetween,vOut):
        self.CommandToClient(self.AgentId,'hud','DoFadeInOut('+str(vIn)+','+str(vBetween)+','+str(vOut)+')','')

    def SetSendMouseClickEvents(self,vbSendEvents):
        if(vbSendEvents):
            self.CommandToClient(self.AgentId,'client','mousebtns','1')
        else:
            self.CommandToClient(self.AgentId,'client','mousebtns','0')
    def SetSendMouseWheelEvents(self,vbSendEvents):
        if(vbSendEvents):
            self.CommandToClient(self.AgentId,'client','mousewheel','1')
        else:
            self.CommandToClient(self.AgentId,'client','mousewheel','0')
            
    def rexIKSetLimbTarget(self, vLimbId, vDest, vTimeToTarget, vStayTime, vConstraintAngle, vStartAnim, vTargetAnim, vEndAnim):
        self.MyWorld.CS.rexIKSetLimbTarget(self.AgentId,vLimbId, vDest, vTimeToTarget, vStayTime, vConstraintAngle, vStartAnim, vTargetAnim, vEndAnim)

    def rexPlayAvatarAnim(self, vAnimName, vRate, vFadeIn, vFadeOut, nRepeats, vbStopAnim):
        self.MyWorld.CS.rexPlayAvatarAnim(self.AgentId,vAnimName, vRate, vFadeIn, vFadeOut, nRepeats, vbStopAnim)

    def rexPlayMeshAnimation(self,vAnimName,vRate,vbLooped, vbStopAnim):
        if(vbLooped):
            return self.MyWorld.CS.rexPlayAvatarAnim(self.Id,vAnimName,vRate,0,0,9999999,False)
        else:
            return self.MyWorld.CS.rexPlayAvatarAnim(self.Id,vAnimName,vRate,0,0,1,False)

    def rexSetAvatarMorph(self, vMorphName, vWeight, vTime):
        self.MyWorld.CS.rexSetAvatarMorph(self.AgentId,vMorphName, vWeight, vTime)

    def rexSetFog(self,vStart,vEnd,vR,vG,vB):
        self.MyWorld.CS.rexSetFog(self.AgentId,vStart,vEnd, vR,vG,vB)

    def rexSetWaterHeight(self,vHeight):
        self.MyWorld.CS.rexSetWaterHeight(self.AgentId,vHeight)
        
    def rexDrawWater(self,draw):
        self.MyWorld.CS.rexDrawWater(self.AgentId,draw)

    def rexSetPostProcess(self,vEffectId,vbToggle):
        self.MyWorld.CS.rexSetPostProcess(self.AgentId,vEffectId,vbToggle)

    def rexRttCamera(self,command,vName,vTex,vPos,vLookAt,width,height):
        self.MyWorld.CS.rexRttCamera(self.AgentId,command,vName,vTex,vPos,vLookAt,width,height)

    def rexRttCameraWorld(self,command,vName,vTex,vPos,vLookAt,width,height):
        self.MyWorld.CS.rexRttCameraWorld(self.AgentId,command,vName,vTex,vPos,vLookAt,width,height)

    def rexSetViewport(self,command,vName,vX,vY,vWidth,vHeight):
        self.MyWorld.CS.rexSetViewport(self.AgentId,command,vName,vX,vY,vWidth,vHeight)

    def rexSetAvatarOverrideAddress(self,vNewAddress):
        self.MyWorld.CS.rexSetAvatarOverrideAddress(self.AgentId,vNewAddress)

    def rexToggleWindSnd(self,vbDisabled):
        self.MyWorld.CS.rexToggleWindSound(self.AgentId,vbDisabled)

    def rexSetCameraClientSideEffectByUUID(self,enable,assetId,vPos,vRot):
        self.MyWorld.CS.rexSetCameraClientSideEffect(self.AgentId,enable,assetId,vPos,vRot)

    def rexSetCameraClientSideEffect(self,enable,assetName,assetType,vPos,vRot):
        self.MyWorld.CS.rexSetCameraClientSideEffect(self.AgentId,enable,assetName,assetType,vPos,vRot)

    def rexSetAmbientLight(self,lightDirection,lightColour,ambientColour):
        self.MyWorld.CS.rexSetAmbientLight(self.AgentId,lightDirection,lightColour,ambientColour)

    def rexSetSky(self,type,images,curvature,tiling):
        self.MyWorld.CS.rexSetSky(self.AgentId,type,images,curvature,tiling)

    def rexPlayFlashAnimationByUUID(self,assetId,left,top,right,bottom, timeToDeath):
        self.MyWorld.CS.rexPlayFlashAnimation(self.AgentId,assetId,left,top,right,bottom, timeToDeath)

    def rexPlayFlashAnimation(self,name,left,top,right,bottom, timeToDeath):
        self.MyWorld.CS.rexPlayFlashAnimation(self.AgentId,name,49,left,top,right,bottom, timeToDeath)

    def rexPreloadAssets(self,vAssetList):
        self.MyWorld.CS.rexPreloadAssets(self.AgentId,vAssetList)

    def rexPreloadAvatarAssets(self,vAssetList):
        self.MyWorld.CS.rexPreloadAvatarAssets(self.AgentId,vAssetList)

    def rexForceFOV(self, fov, enable):
        self.MyWorld.CS.rexForceFOV(self.AgentId, fov, enable)

	# Force camera mode
	# mode 0 = no limitations 1 = force 1st person 3 = force 3rd person
	# min and max zooms range from 0.0 to 1.0. Viewer default (full range) = min 0.0 max 1.0
    def rexForceCamera(self, mode, minzoom, maxzoom):
        self.MyWorld.CS.rexForceCamera(self.AgentId, mode, minzoom, maxzoom)

    # Senses
    # Blindness, 0 = normal visibility, 100=full blindness, types 0-2
    def SetBlindness(self,vType,vLevel):
        self.CommandToClient(self.AgentId,'hud','SetBlindness('+ str(vType) +','+  str(vLevel)+')','')
    # Deaf, True or False
    def SetDeaf(self,vbMakeDeaf):
        if(vbMakeDeaf):
            self.CommandToClient(self.AgentId,'client','deaf','1')
        else:
            self.CommandToClient(self.AgentId,'client','deaf','0')
    # Mute, True or False
    def SetMute(self,vbMute):
        if(vbMute):
            self.CommandToClient(self.AgentId,'client','mute','1')
        else:
            self.CommandToClient(self.AgentId,'client','mute','0')
