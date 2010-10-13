import rxactor
import sys
import clr


class A(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "startuptest.A"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.MyWorld.MyEventManager.onClientStartup += self.HandleOnClientStartup
        print "startuptest.A created"

    def EventDestroyed(self):
        self.MyWorld.MyEventManager.onClientStartup -= self.HandleOnClientStartup

    def HandleOnClientStartup(self,vAvatar,vStatus):
        try:
            self.llShout(0,"Startup event recived from avatar "+str(vAvatar)+ " and status is "+str(vStatus))
            print "Startup event recived from avatar "+str(vAvatar)+ " and status is "+str(vStatus)
        except:
            print "startuptest,HandleOnClientStartup", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]




        
        
