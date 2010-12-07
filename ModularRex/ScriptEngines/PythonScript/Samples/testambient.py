import rxactor
import rxavatar
import rxworldinfo
import sys
import math
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

class Ambient(rxworldinfo.WorldInfo):
    @staticmethod
    def GetScriptClassName():
        return "testambient.Ambient"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.MyWorld.MyEventManager.onAddPresence += self.handleOnAddPresence
        print "testambient.Ambient EventCreated"

    def EventDestroyed(self):
        self.MyWorld.MyEventManager.onAddPresence -= self.handleOnAddPresence
        super(Ambient,self).EventDestroyed()

    def handleOnAddPresence(self,vAvatar):
        vAvatar.rexSetAmbientLight(Vector3(0,0,1), Vector3(0.1,0.1,0.1), Vector3(0.1,0.1,0.1))

