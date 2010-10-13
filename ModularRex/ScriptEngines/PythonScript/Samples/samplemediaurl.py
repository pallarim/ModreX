import rxactor
import rxavatar
import sys
import clr

import random
import math


class MediaUrlTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplemediaurl.MediaUrlTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplemediaurl.MediaUrlTest EventCreated"

    def EventTouch(self,vAvatar):
        self.llShout(0,"Changeing MediaURL to http://www.realxtend.org/")
        self.rexSetTextureMediaURL("http://www.realxtend.org/", 1)
