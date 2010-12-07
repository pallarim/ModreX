import rxactor
import rxavatar
import sys
import clr

import random
import math


class AnimTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavataranim.AnimTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleavataranim.AnimTest EventCreated"

    def EventTouch(self,vAvatar):
        vAvatar.rexPlayAvatarAnim("Hover",0.3,0.4,0.4,4,False)
        self.llShout(0,"Custom animation!")

        
        


