import rxactor
import rxavatar
import sys
import clr

import random
import math


class Water(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplewater.Water"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplewater.Water EventCreated"

    def EventTouch(self,vAvatar):
        self.CurrentTest = 0
        self.Agent = vAvatar
        self.SetTimer(8,True)
        self.ChangeWater()
        
    def EventTimer(self):
        self.ChangeWater()
        
    def ChangeWater(self):
        if(self.CurrentTest > 5):
            self.SetTimer(0,False)
            self.llShout(0,"Water test ended")
            return

        self.CurrentTest = self.CurrentTest+1

        waterf = 10 + random.random()*100

        stext = "Water height set to:" + str(waterf)
        self.llShout(0,stext)
        self.Agent.rexSetWaterHeight(waterf)




