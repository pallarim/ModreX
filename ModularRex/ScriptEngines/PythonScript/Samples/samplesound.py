import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')

class SoundTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplesound.SoundTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplesound.SoundTest EventCreated"
        self.Status = 0

    def EventTouch(self,vAvatar):
        if(self.Status == 0):
		    self.PlayClientSound(vAvatar.AgentId,"musicsnd",1)
		    self.llShout(0, "PlayClientSound, avatar as source")
        elif(self.Status == 1):
		    self.llPlaySound("b2912295-0d02-4332-a940-5679b14683e7",1)
		    self.llShout(0, "llPlaySound, prim as source")

        self.Status += 1
        if(self.Status > 2):
            self.Status = 0

		
		
