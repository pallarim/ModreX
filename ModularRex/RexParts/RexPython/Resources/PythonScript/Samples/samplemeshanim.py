import rxactor
import rxavatar
import sys
import clr

import random
import math


class AnimTest(rxactor.Actor):
    def GetScriptClassName():
        return "samplemeshanim.AnimTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplemeshanim.AnimTest EventCreated"

    def EventTouch(self,vAvatar):
        self.rexPlayMeshAnimation(self.Id,"Wave",0.3,False,False)
        self.llShout(0,"Custom animation!")

