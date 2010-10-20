import rxactor
import sys
import clr


class Test(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "avatarspeedtest.Test"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "avatarspeedtest.Test created"

    def EventTouch(self,vAvatar):
        curvalue = vAvatar.GetMovementModifier()
        if curvalue == 1.0:
            curvalue = 2.0
        else:
            curvalue = 1.0
        vAvatar.SetMovementModifier(curvalue)
        self.llShout(0,"Your avatar speed is set to "+str(curvalue))




        
        
