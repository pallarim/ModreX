import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class RttCam(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplertt.RttCam"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplertt.RttCam EventCreated"
        self.command = 0 # 0 - remove, 1 - add

    def EventTouch(self,vAvatar):
        if(self.command == 0):
            self.command = 1
            self.llShout(0,"Creating render to texture camera.")
        else:
            self.command = 0
            self.llShout(0,"Removing render to texture camera.")
        pos = self.llGetPos()
        lookAt = pos + Vector3(-1, 0, 0) * self.llGetRot()
        cameraName = "sample_cam"
        textureId = "00000000-0000-1111-9999-000000000001" #opensim brickwall texture
        textureWidth = 256
        textureHeight = 256
        vAvatar.rexRttCameraWorld(self.command,cameraName,textureId,pos,lookAt, textureWidth, textureHeight)
