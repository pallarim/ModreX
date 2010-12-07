import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class AllHere(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "avatarutils.AllHere"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        print "avatarutils.ComeHere EventCreated"

    def EventTouch(self,vAvatar):
        self.llShout(0,"Calling all avatars here")
        
        teleloc = self.llGetPos() + Vector3(0,0,6)
        for k, v in self.MyWorld.AllAvatars.iteritems():
            v.DoLocalTeleport(teleloc)
            teleloc = teleloc + Vector3(0,0,6)
        

        

