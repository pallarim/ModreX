import rxactor
import sys
import clr


class WalkDisabler(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavatarmovemods.WalkDisabler"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleavatarmovemods.WalkDisabler created"

    def EventTouch(self,vAvatar):
        curvalue = vAvatar.GetWalkDisabled()
        curvalue = not curvalue
        vAvatar.SetWalkDisabled(curvalue)
        self.llShout(0,"Your walkdisabled is set to:"+str(curvalue))

class FlyDisabler(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavatarmovemods.FlyDisabler"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleavatarmovemods.FlyDisabler created"

    def EventTouch(self,vAvatar):
        curvalue = vAvatar.GetFlyDisabled()
        curvalue = not curvalue
        vAvatar.SetFlyDisabled(curvalue)
        self.llShout(0,"Your flydisabled is set to:"+str(curvalue))
        


        
        
