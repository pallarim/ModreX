import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class Test(rxactor.Actor):
    def GetScriptClassName():
        return "scriptfunctiontest.Test"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        self.MyTimer = self.CreateRexTimer(4,10)
        self.MyTimer.onTimer += self.HandleTimer
        
        #change these values to be suitable in your test environment
        self.__meshId = "82bfa22b-02b6-b0aa-71fb-799b29e061dc"
        self.__particleScriptId = "676e66ce-c788-1b5a-5bef-50477f13c60a"

        print "scriptfunctiontest.Test EventCreated"

    def EventDestroyed(self):
        print "scriptfunctiontest.Test EventDestroyed"
        del self.MyTimer
        self.MyTimer = None

        super(self.__class__,self).EventDestroyed()

    def EventTouch(self,vAvatar):
        if(self.MyTimerCount > 0):
            self.llShout(0,"Test already running...")
            return

        self.llShout(0,"Starting test")
        self.CurrentTest = 0
        self.AgentId = vAvatar.AgentId
        self.MyTimer.Start()

    def EventTick(self,vDeltaTime):
        self.TickCount += 1
        textstr = "Tickcount:" + str(self.TickCount) + " delta:" + str(vDeltaTime)
        self.llShout(0,textstr)

    def HandleTimer(self):
        self.CurrentTest = self.CurrentTest+1
        eval("self.Test"+str(self.CurrentTest)+"()")

    #SetRexDrawDistance
    def Test1(self):
        self.SetRexDrawDistance(5.0)

    #GetRexDrawDistance
    def Test2(self):
        vDist = self.GetRexDrawDistance()
        if vDist != 5.0 :
            self.llSay(0, "SetRexDrawDistance - GetRexDrawDistance test failed")
        else:
            self.llSay(0, "test 1 & 2 passed")

    #SetRexLOD
    def Test3(self):
        self.SetRexLOD(3.0)

    #GetRexLOD
    def Test4(self):
        vLod = self.GetRexLOD()
        if vLod != 3.0 :
            self.llSay(0, "SetRexLOD - GetRexLOD test failed")
        else :
            self.llSay(0, "test 3 & 4 passed")

    #SetRexMeshUUID
    def Test5(self):
        self.SetRexMeshUUID(self.__meshId)

    #GetRexMeshUUID
    def Test6(self):
        vAssetId = self.GetRexMeshUUID()
        if vAssetId == self.__meshId:
            self.llSay(0, "test 5 & 6 passed")
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")

    #SetRexCollisionMeshUUID
    def Test7(self):
        self.SetRexCollisionMeshUUID(self.__meshId)

    #GetRexCollisionMeshUUID
    def Test8(self):
        vAssetId = self.GetRexCollisionMeshUUID()
        if vAssetId == self.__meshId:
            self.llSay(0, "test 7 & 8 passed")
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")

    #SetRexParticleScriptUUID
    def Test9(self):
        self.SetRexParticleScriptUUID(self.__particleScriptId)

    #GetRexParticleScriptUUID
    def Test10(self):
        vAssetId = self.GetRexParticleScriptUUID()
        if vAssetId == self.__particleScriptId:
            self.llSay(0, "test 9 & 10 passed")
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")
