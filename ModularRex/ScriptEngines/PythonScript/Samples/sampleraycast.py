import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class RayTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleraycast.RayTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleraycast.RayTest"

    def EventTouch(self,vAvatar):
        if(self.MyTimerCount > 0):
            self.SetTimer(0,False)
            self.llShout(0,"Stop raytrace")
        else:
            self.llShout(0,"Start raytrace")
            self.AgentId = vAvatar.AgentId
            self.SetTimer(1,True)
        
    def EventTimer(self):
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]

        avatarrot = tempavatar.llGetRot()
        avatarforward = self.llRot2Fwd(avatarrot)
        startloc = tempavatar.llGetPos()
        colobj = self.rexRaycast(startloc,avatarforward,10,tempavatar.Id)

        if(colobj == None):
            self.llShout(0,"Nothing")
        elif isinstance(colobj,rxavatar.Avatar):
            self.llShout(0,"Avatar named "+colobj.GetFullName())
        else:
            self.llShout(0,"Prim id:" +  colobj.Id  + ", name "+colobj.llGetObjectName())
