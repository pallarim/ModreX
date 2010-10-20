import rxactor
import rxavatar
import sys
import clr

import random
import math


class ForceFOVTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleforcefov.ForceFOVTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.enable = True
        print "sampleforcefov.ForceFOVTest EventCreated"

    def EventTouch(self,vAvatar):
        if self.enable == True:
            self.llShout(0,"Forcing FOV to 30 degrees!")
            vAvatar.rexForceFOV(30.0,True)
            self.enable = False
        else:
            self.llShout(0,"Resetting FOV to normal!")
            # Note that the FOV parameter is irrelevant when disabling override
            vAvatar.rexForceFOV(30.0,False)
            self.enable = True
