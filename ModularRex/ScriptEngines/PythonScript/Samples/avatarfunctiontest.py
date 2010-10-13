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
        return "avatarfunctiontest.Test"

    def EventCreated(self):
        #change these values to be suitable in your test environment

        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        self.MyTimer = self.CreateRexTimer(4,14)
        self.MyTimer.onTimer += self.HandleTimer
        
        self._testsRun = 0
        self._testsOK = 0

        print "avatarfunctiontest.Test EventCreated"

    def EventDestroyed(self):
        print "avatarfunctiontest.Test EventDestroyed"
        del self.MyTimer
        self.MyTimer = None

        super(self.__class__,self).EventDestroyed()

    def EventTouch(self,vAvatar):
        if(self.MyTimerCount > 0):
            self.llShout(0,"Test already running...")
            return
        self.Avatar = vAvatar
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

    #SP tests
    
    #SP Get*Name
    def Test1(self):
        vFull = self.Avatar.GetFullName()
        vFirst = self.Avatar.GetFirstName()
        vLast = self.Avatar.GetLastName()

        sum = vFirst + " " + vLast
        
        if sum == vFull:
            self.llSay(0, "test 1 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 1 failed. FullName="+vFull+" sum="+sum)

    #SP GetPos & LocalTeleport
    def Test2(self):
        fPos = self.Avatar.llGetPos()
        self.Avatar.llSetPos(Vector3(20, 20, 100))
        sPos = self.Avatar.llGetPos()

        if fPos != sPos:
            self.Avatar.llSetPos(fPos)
            self.llSay(0, "test 2 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 2 failed.")

    #SPSetMovementModifier
    def Test3(self):
        self.Avatar.SetMovementModifier(2.0)
        

    #SPGetMovementModifier
    def Test4(self):
        vModifier = self.Avatar.GetMovementModifier()
        if vModifier == 2.0:
            self.llSay(0, "test 3 & 4 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 3 & 4 failed.")
            
    #SPSetRot
    def Test5(self):
        self._1stRot = self.Avatar.llGetRot()
        self._rotresult = self.llEuler2Rot(Vector3(0,0,math.pi*0.5))
        self.Avatar.llSetRot(self._rotresult)

    #SPGetRot
    def Test6(self):
        vRot = self.Avatar.llGetRot()
        if vRot.ToString() == self._rotresult.ToString():  #if not done with string comperason, the result would fail
            self.llSay(0, "test 5 & 6 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 5 & 6 failed. vRot="+vRot.ToString()+" rotR="+self._rotresult.ToString())
            self.llSay(0, "1st rot="+self._1stRot.ToString())
            
    def Test7(self):
        self.Avatar.SetWalkDisabled(True)
        
    def Test8(self):
        vbWalk = self.Avatar.GetWalkDisabled()
        if vbWalk:
            self.llSay(0, "test 7 & 8 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 7 & 8 failed.")
            
            
    def Test9(self):
        self.Avatar.SetFlyDisabled(True)

    def Test10(self):
        vbFly = self.Avatar.GetFlyDisabled()
        if vbFly:
            self.llSay(0, "test 9 & 10 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 9 & 10 failed.")
            
    def Test11(self):
        self.Avatar.SetVertMovementModifier(0.8)
        
    def Test12(self):
        vMod = self.Avatar.GetVertMovementModifier()
        
        if vMod == 0.8:
            self.llSay(0, "test 11 & 12 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 11 & 12 failed.")
            
    def Test13(self):
        self.Avatar.SetSitDisabled(True)
        
    def Test14(self):
        vDis = self.Avatar.GetSitDisabled()
        
        if vDis :
            self.llSay(0, "test 13 & 14 passed")
            self._testsOK += 2
        else:
            self.llSay(0, "test 13 & 14 failed.")
