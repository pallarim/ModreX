import rxactor
import sys
import clr


class SitTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sittest.SitTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sittest.SitTest created"

    def EventTouch(self,vAvatar):
        curvalue = vAvatar.GetSitDisabled()
        curvalue = not curvalue
        vAvatar.SetSitDisabled(curvalue)
        self.llShout(0,"Your sitdisabled is set to:"+str(curvalue))




        
        
