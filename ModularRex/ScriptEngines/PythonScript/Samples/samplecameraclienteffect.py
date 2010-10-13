import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class CameraEffect(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplecameraclienteffect.CameraEffect"

    def EventCreated(self):
        self.status = 0
        super(self.__class__,self).EventCreated()
        print "samplecameraclienteffect.CameraEffect EventCreated"

    def EventTouch(self,vAvatar):
        enable = True
        if (self.status >= 1):
            enable = False
            self.status = 0
        else:
            self.status = self.status + 1
        self.llShout(0,"fire!")
        assetName = "smoke_small"
        assetType = 47
        offsetPos = Vector3(0,0,1)
        offsetRot = self.llEuler2Rot(Vector3(0,0,0))
        vAvatar.rexSetCameraClientSideEffect(enable, assetName, assetType, offsetPos, offsetRot)

