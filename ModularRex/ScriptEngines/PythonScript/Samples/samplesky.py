import rxactor
import rxavatar
import rxworldinfo
import sys
import math
import clr

class Sky(rxworldinfo.WorldInfo):
    @staticmethod
    def GetScriptClassName():
        return "samplesky.Sky"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.MyWorld.MyEventManager.onAddPresence += self.handleOnAddPresence
        print "samplesky.Sky EventCreated"

    def EventDestroyed(self):
        self.MyWorld.MyEventManager.onAddPresence -= self.handleOnAddPresence
        super(Sky,self).EventDestroyed()

    def handleOnAddPresence(self,vAvatar):
        vAvatar.rexSetSky(1, "f08f085f-2396-49b1-b119-38eea771e54b_up b1f1da03-e22a-4108-a2c3-2f14fbe8a5a7_fr 02b85aa3-8ebe-44cf-950b-790b53a8b3f2_bk ca66566d-3db9-4889-b8d7-a0dc8c173c80_rt 52af4cc3-3a48-4248-9019-9efcf1657ffa_lf e9caee12-4cde-4f50-bef0-c7f105d31012_dn", 60, 1)

