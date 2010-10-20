import rxactor
import sys
import clr


class Test(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "avatargmtest.Test"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "avatargmtest.Test created"

    def EventTouch(self,vAvatar):
        vAvatar.AddGenericMessageHandler("RexMediaUrl", self.MyHandler)
		
    def MyHandler(self,sender, method, args):
        self.llShout(0,"Received gm "+str(method))




        
        
