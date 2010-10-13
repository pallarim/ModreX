import rxactor
import rxavatar
import sys
import clr

import random
import math


class FlashAnim(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleflashanimation.FlashAnim"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleflashanimation.FlashAnim EventCreated"

    def EventTouch(self,vAvatar):
        left = 0.1
        top = 0.1
        right = 0.9
        bottom = 0.9
        timeToDeath = 5.0 # in seconds
        vAvatar.rexPlayFlashAnimation("cakedance",left,top,right,bottom,timeToDeath)
