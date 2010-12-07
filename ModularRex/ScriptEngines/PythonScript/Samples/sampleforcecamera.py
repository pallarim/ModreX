import rxactor
import rxavatar
import sys
import clr

import random
import math


class ForceCameraTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleforcecamera.ForceCameraTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.phase = 0
        print "sampleforcecamera.ForceCameraTest EventCreated"

    def EventTouch(self,vAvatar):
        if self.phase == 0:
            self.llShout(0,"Forcing 1st person mode!")
            vAvatar.rexForceCamera(1, 0.0, 1.0)
            self.phase = 1
        elif self.phase == 1:
            self.llShout(0,"Forcing 3rd person mode with fixed zoom!")
            vAvatar.rexForceCamera(3, 0.5, 0.5)
            self.phase = 2
        elif self.phase == 2:
            self.llShout(0,"Removing camera limits!")
            vAvatar.rexForceCamera(0, 0.0, 1.0)
            self.phase = 0
