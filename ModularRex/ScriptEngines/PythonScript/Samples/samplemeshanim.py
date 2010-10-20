import rxactor
import rxavatar
import sys
import clr

import random
import math

# This tests a primitive with a mesh and an animation named Wave.
# For avatar animation see sampleavataranim.py
class AnimTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplemeshanim.AnimTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplemeshanim.AnimTest EventCreated"

    def EventTouch(self,vAvatar):
        self.rexPlayMeshAnimation("Wave",0.3,False,False)
        self.llShout(0,"Custom animation!")

