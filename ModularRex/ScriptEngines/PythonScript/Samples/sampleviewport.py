import rxactor
import rxavatar
import sys
import clr

import random
import math


class CViewport(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleviewport.CViewport"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.command = 0 # 0 - remove, 1 - add
        print "sampleviewport.CViewport EventCreated"

    def EventTouch(self,vAvatar):
        if(self.command == 0):
            self.command = 1
            self.llShout(0,"Creating viewport.")
        else:
            self.command = 0
            self.llShout(0,"Removing viewport.")
        viewportName = "sample_cam"
        posX = 0.82
        posY = 0.4
        width = 0.18
        height = 0.19
        vAvatar.rexSetViewport(self.command,viewportName,posX,posY,width,height)
