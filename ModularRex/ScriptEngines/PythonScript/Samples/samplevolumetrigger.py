import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

class SayHello(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplevolumetrigger.SayHello"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.SetUsePrimVolumeCollision(True)
        self.MyAvatars = {}
        
    # This event triggers every 1 second
    # It's enough to send text to avatar every 6 seconds
    def EventPrimVolumeCollision(self,vOther):
        if isinstance(vOther,rxavatar.Avatar):
            if self.MyAvatars.has_key(vOther.AgentId):
                if(self.GetTime() > self.MyAvatars[vOther.AgentId]):
                    self.ShowMyTextToAvatar(vOther)
            else:
                self.ShowMyTextToAvatar(vOther)

    def ShowMyTextToAvatar(self,vAvatar):
        self.MyAvatars[vAvatar.AgentId] = self.GetTime()+6
        vAvatar.ShowTutorialBox("This is a nice place to stand for a while",9)


class HandShakeArea(rxactor.Actor):
    def GetScriptClassName():
        return "samplevolumetrigger.HandShakeArea"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.SetUsePrimVolumeCollision(True)
        self.NextShakeTime = 0

    def EventPrimVolumeCollision(self,vOther):
        if(self.GetTime() > self.NextShakeTime):
            self.NextShakeTime = self.GetTime()+5

            templist = self.GetRadiusAvatars(4)
            
            allpos = Vector3(0,0,0.1)
            if(templist.Count > 1):
                for i in templist:
                    tempavatar = self.MyWorld.AllActors[i]
                    allpos = allpos + tempavatar.llGetPos()
            else:
                tempavatar = self.MyWorld.AllActors[templist[0]]
                avatarforward = tempavatar.llRot2Fwd(tempavatar.llGetRot())
                avatarleft = tempavatar.llRot2Left(tempavatar.llGetRot())
                allpos = tempavatar.llGetPos() + (avatarforward*0.7) + (avatarleft*-0.3)
                
            targetpos = (allpos / templist.Count) + Vector3(0,0,0.1)

            for i in templist:
                tempavatar = self.MyWorld.AllActors[i]
                tempavatar.rexIKSetLimbTarget(0,targetpos,1,1,173,'','Handshake','')
            del templist


class HighFiveArea(rxactor.Actor):
    def GetScriptClassName():
        return "samplevolumetrigger.HighFiveArea"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.SetUsePrimVolumeCollision(True)
        self.NextShakeTime = 0

    def EventPrimVolumeCollision(self,vOther):
        if(self.GetTime() > self.NextShakeTime):
            self.NextShakeTime = self.GetTime()+5
            
            templist = self.GetRadiusAvatars(4)
            for i in templist:
                tempavatar = self.MyWorld.AllActors[i]
                avatarforward = tempavatar.llRot2Fwd(tempavatar.llGetRot())
                avatarleft = tempavatar.llRot2Left(tempavatar.llGetRot())
                targetpos = tempavatar.llGetPos() + Vector3(0,0,0.8) + (avatarforward*0.2) + (avatarleft*-0.35)
                tempavatar.rexIKSetLimbTarget(0,targetpos,0.7,0.7,150,'','HighFive','')
            del templist
            self.SetTimer(0.55,False)
        
    def EventTimer(self):
        bTargetSet = False
        templist = self.GetRadiusAvatars(4)
        for i in templist:
            tempavatar = self.MyWorld.AllActors[i]
            if not bTargetSet:
                avatarforward = tempavatar.llRot2Fwd(tempavatar.llGetRot())
                avatarleft = tempavatar.llRot2Left(tempavatar.llGetRot())
                targetpos = tempavatar.llGetPos() + Vector3(0,0,0.85) + (avatarforward*0.36) + (avatarleft*-0.3)
                bTargetSet = True
            tempavatar.rexIKSetLimbTarget(0,targetpos,0.4,0.3,150,'','HighFive','')
        del templist


