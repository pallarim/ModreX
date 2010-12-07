import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class EffectTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclienteffect.EffectTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleclienteffect.EffectTest EventCreated"

    def EventTouch(self,vAvatar):
        self.CurrentTest = 0
        self.AgentId = vAvatar.AgentId

        self.llShout(0,"fire!")
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]

        self.Spawnloc = tempavatar.llGetPos()
        self.Spawnrot = Vector3(1,0,0) * tempavatar.llGetRot()
        pos = self.Spawnloc + 0.4 * self.Spawnrot
        rot = self.llEuler2Rot(Vector3(0,math.pi*0.5,0))
        duration = 2.5
        speed = 5.5
        #vAvatar.rexSetClientSideEffect("smoke_small",47,0,8.5,pos, tempavatar.llGetRot() * rot,4.5)
        vAvatar.rexSetClientSideEffect("smoke_small",47,0,duration,pos,tempavatar.llGetRot(),speed)
        # distance to travel is speed * time
        pos = self.Spawnloc + speed * duration * self.Spawnrot
        vAvatar.rexSetClientSideEffect("fire_small",47,duration,8.201,pos,self.llEuler2Rot(Vector3(0,0,0)),0)
