import rxactor
import rxavatar
import sys
import clr
import random
import math


class MorphTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavatarmorph.MorphTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.CurrentWeight = 1.0
        print "sampleavatarmorph.MorphTest EventCreated"

    def EventTouch(self,vAvatar):
        vAvatar.rexSetAvatarMorph("fat-body-upper",self.CurrentWeight,0.5)
        vAvatar.rexSetAvatarMorph("fat-body-lower",self.CurrentWeight,0.5)
        vAvatar.rexSetAvatarMorph("fat-arms-upper",self.CurrentWeight,0.5)
        vAvatar.rexSetAvatarMorph("fat-arms-lower",self.CurrentWeight,0.5)
        vAvatar.rexSetAvatarMorph("fat-legs-upper",self.CurrentWeight,0.5)
        vAvatar.rexSetAvatarMorph("fat-legs-lower",self.CurrentWeight,0.5)
        self.llShout(0,"Morph animation!")
        # Switch between full & zero weight each time
        self.CurrentWeight = 1.0 - self.CurrentWeight


class MasterModifierTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavatarmorph.MasterModifierTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.CurrentWeight = 1.0
        print "sampleavatarmorph.MasterModifierTest EventCreated"

    def EventTouch(self,vAvatar):
        vAvatar.rexSetAvatarMorph("Body mass",self.CurrentWeight,0.5)
        self.llShout(0,"Master modifier animation!")
        # Switch between full & 0.5 weight each time
        if self.CurrentWeight == 1.0:
        	self.CurrentWeight = 0.5
        else:
        	self.CurrentWeight = 1.0


