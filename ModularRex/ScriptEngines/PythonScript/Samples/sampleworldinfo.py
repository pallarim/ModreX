import rxactor
import rxavatar
import rxworldinfo
import sys
import clr
import math

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

class WI(rxworldinfo.WorldInfo):
    @staticmethod
    def GetScriptClassName():
        return "sampleworldinfo.WI"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.MyWorld.MyEventManager.onAddPresence += self.handleOnAddPresence

    def EventDestroyed(self):
        self.MyWorld.MyEventManager.onAddPresence -= self.handleOnAddPresence
        super(WI,self).EventDestroyed()

    def handleOnAddPresence(self,vAvatar):
        vAvatar.DoLocalTeleport(Vector3(154.419,122.628,23))
