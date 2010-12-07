import rxactor
import rxavatar
import sys
import clr

import random
import math


class PP(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplepostprocess.PP"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplepostprocess.PP EventCreated"

    def EventTouch(self,vAvatar):
        self.CurrentTest = -1
        self.Agent = vAvatar
        
        self.MyTimer = self.CreateRexTimer(7,14)
        self.MyTimer.onTimer += self.HandleTimer
        self.MyTimer.Start()
        self.ChangeEffect()
        
    def HandleTimer(self):
        self.ChangeEffect()
        
    def ChangeEffect(self):
        if(self.CurrentTest == 13):
            self.llShout(0,"Postprocess test ended")
            return

        self.CurrentTest += 1
        stext = "Postprocess set to:" + str(self.CurrentTest)
        self.llShout(0,stext)
        
        # Turn off old
        if(self.CurrentTest > 0):
            self.Agent.rexSetPostProcess(self.CurrentTest-1,False)
        # Turn on new
        self.Agent.rexSetPostProcess(self.CurrentTest,True)
        


class AutoBloom(rxactor.Actor):
    def GetScriptClassName():
        return "samplepostprocess.AutoBloom"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.MyWorld.MyEventManager.onAddPresence += self.handleOnAddPresence

    def EventDestroyed(self):
        self.MyWorld.MyEventManager.onAddPresence -= self.handleOnAddPresence
        super(AutoBloom,self).EventDestroyed()

    def handleOnAddPresence(self,vAvatar):
        vAvatar.rexSetPostProcess(1,True)






