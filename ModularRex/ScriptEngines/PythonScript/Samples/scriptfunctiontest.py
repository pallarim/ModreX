import rxactor
import rxavatar
import sys
import clr
from System.Reflection import MethodBase

logAsm = clr.LoadAssemblyByName('log4net')

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class Test(rxactor.Actor):
    def __init__(self,vId):
        self.log = logAsm.log4net.LogManager.GetLogger(type(MethodBase.GetCurrentMethod().DeclaringType))
        super(Test, self).__init__(vId)

    @staticmethod
    def GetScriptClassName():
        return "scriptfunctiontest.Test"

    def EventCreated(self):
        #change these values to be suitable in your test environment
        self.__meshId = "50dae2f8-e1c9-7d28-2ff7-0cad68c9db97"  #jack mesh
        self.__particleScriptId = "676e66ce-c788-1b5a-5bef-50477f13c60a"
        self.__animationId = "05aaa369-d602-afec-61a6-407a6cdbbd9f"
        self.__animationName = "Wave"

        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        self.MyTimer = self.CreateRexTimer(4,33)
        self.MyTimer.onTimer += self.HandleTimer
        
        self._testsRun = 0
        self._testsOK = 0

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

        self._testsRun = 0
        self._testsOK = 0
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
        self._testsRun += 1
        self.log.Info("[PYTHON]: Tests passed: "+ str(self._testsOK) +"/"+ str(self._testsRun))

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
            self._testsOK += 2

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
            self._testsOK += 2

    #SetRexMeshUUID
    def Test5(self):
        self.SetRexMeshUUID(self.__meshId)

    #GetRexMeshUUID
    def Test6(self):
        vAssetId = self.GetRexMeshUUID()
        if vAssetId == self.__meshId:
            self.llSay(0, "test 5 & 6 passed")
            self._testsOK += 2
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
            self._testsOK += 2
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
            self._testsOK += 2
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")
    
    #SetRexAnimationPackageUUID
    def Test11(self):
        self.SetRexAnimationPackageUUID(self.__animationId)
        
    #GetRexAnimationPackageUUID 
    def Test12(self):
        vAssetId = self.GetRexAnimationPackageUUID()
        if vAssetId == self.__animationId :
            self.llSay(0, "test 11 & 12 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")

    #SetRexAnimationName
    def Test13(self):
        self.SetRexAnimationName(self.__animationName)

    #GetRexAnimationName 
    def Test14(self):
        vAssetId = self.GetRexAnimationName()
        if vAssetId == self.__animationName :
            self.llSay(0, "test 13 & 14 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")

    #SetRexAnimationRate
    def Test15(self):
        self.SetRexAnimationRate(2)

    #GetRexAnimationRate
    def Test16(self):
        vRate = self.GetRexAnimationRate()
        if vRate == 2 :
            self.llSay(0, "test 15 & 16 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test failed, asset "+vAssetId+" found")

    #RexGetMaterialCount
    def Test17(self):
        self.__materialCount = self.RexGetMaterialCount()
        if self.__materialCount == 2:
            self.llSay(0, "test 17 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test RexGetMaterialCount failed. either wrong mesh is used or setting mesh also failed.")
            self.llSay(0, "Number of materials: "+self.__materialCount)

    #RexSetMaterialUUID
    def Test18(self):
        if self.__materialCount >= 1:
            self.RexSetMaterialUUID(0, "00000000-0000-2222-3333-100000001013") #this asset is in OpenSim library

    #RexGetMaterial
    def Test19(self):
        vAsset = self.RexGetMaterial(0)
        if vAsset == "00000000-0000-2222-3333-100000001013":
            self.llSay(0, "test 18 & 19 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test failed, asset "+vAsset+" found")
            
    #SetRexSoundVolume
    def Test20(self):
        self.SetRexSoundVolume(3.0)
    #GetRexSoundVolume
    def Test21(self):
        soundVol = self.GetRexSoundVolume()
        if soundVol == 3.0:
            self.llSay(0, "test 20 & 21 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 20 & 21 failed")

    #SetRexSoundRadius
    def Test22(self):
        self.SetRexSoundRadius(3.0)
    #GetRexSoundRadius
    def Test23(self):
        soundRad = self.GetRexSoundRadius()
        if soundRad == 3.0:
            self.llSay(0, "test 22 & 23 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 22 & 23 failed")
            
    #SetRexSelectPriority
    def Test24(self):
        self.SetRexSelectPriority(3.0)
    #GetRexSelectPriority
    def Test25(self):
        sPrio = self.GetRexSelectPriority()
        if sPrio == 3.0:
            self.llSay(0, "test 24 & 25 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 24 & 25 failed")

    #SetVelocity
    def Test26(self):
        tVel = Vector3(0,0,2)
        self.SetVelocity(tVel)
        vVel = self.llGetVel()

        if tVel == vVel:
            self.llSay(0, "test 26 passed")
            self._testsOK += 1
        else :
            self.llSay(0, "test 26 failed. vVel="+vVel.ToString())

    #SetPhysics
    def Test27(self):
        self.SetVelocity(Vector3(0,0,0))
        self.SetPhysics(False)
        
    #GetPhysics
    def Test28(self):
        vbPhys = self.GetPhysics()
        if vbPhys == False :
            self.llSay(0, "test 27 & 28 passed")
            self._testsOK += 2
        else :
            self.llSay(0, "test 27 & 28 failed")
            
    #SendAlertToAvatar
    def Test29(self):
        self.SendAlertToAvatar(self.AgentId, "Test 29 passed", True)
        self._testsOK += 1
        
    #CommandToClient
    def Test30(self):
        self.CommandToClient(self.AgentId, 'hud', 'ShowScrollMessage("Test30 passed", 30)', "")
        self._testsOK += 1

    #GetPrimLocalIdFromUUID
    def Test31(self):
        myUUID = self.llGetKey().ToString()
        vId = self.GetPrimLocalIdFromUUID(myUUID)
        if str(self.Id) == str(vId):
            self.llSay(0, "test 31 passed")
            self._testsOK += 1
        else :
            self.llSay(0, "test 31 failed. vId="+str(vId)+" Id="+str(self.Id))

    #GetRadiusActors
    def Test32(self):
        actors = self.GetRadiusActors(100)
        if len(actors) >= 2:
            self.llSay(0, "test 32 passed")
            self._testsOK += 1
        else :
            self.llSay(0, "test 32 failed")

    #GetRadiusAvatars
    def Test33(self):
        avatars = self.GetRadiusAvatars(100)
        if len(avatars) >= 1:
            self.llSay(0, "test 33 passed")
            self._testsOK += 1
        else :
            self.llSay(0, "test 33 failed")
            


