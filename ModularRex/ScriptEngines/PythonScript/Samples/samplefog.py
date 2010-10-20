import rxactor
import rxavatar
import sys
import clr

import random
import math


class Fog(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplefog.Fog"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplefog.Fog EventCreated"

    def EventTouch(self,vAvatar):
        self.CurrentTest = 0
        self.Agent = vAvatar
        self.SetTimer(8,True)
        self.ChangeFog()
        
    def EventTimer(self):
        self.ChangeFog()
        
    def ChangeFog(self):
        if(self.CurrentTest > 5):
            self.SetTimer(0,False)
            self.llShout(0,"Fog test ended")
            return

        self.CurrentTest = self.CurrentTest+1

        startf = 10 + random.random()*50
        endf =  startf + 10 + random.random()*100
        rcolor = random.random()
        gcolor = random.random()
        bcolor = random.random()

        stext = "Fog set to:" + str(startf) + "  " + str(endf) + "  " + str(rcolor) + "  " + str(gcolor) + "  " + str(bcolor)
        self.llShout(0,stext)
        self.Agent.rexSetFog(startf,endf,rcolor,gcolor,bcolor)




