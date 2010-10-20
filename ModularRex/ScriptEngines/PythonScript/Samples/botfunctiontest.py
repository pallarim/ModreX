import rxactor
import rxavatar
import sys
import clr
from System.Reflection import MethodBase
import time

logAsm = clr.LoadAssemblyByName('log4net')

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3
LSLFloat = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.LSLFloat

import random
import math


class Test(rxactor.Actor):
    def __init__(self,vId):
        self.log = logAsm.log4net.LogManager.GetLogger(type(MethodBase.GetCurrentMethod().DeclaringType))
        super(Test, self).__init__(vId)

    @staticmethod
    def GetScriptClassName():
        return "botfunctiontest.Test"

    def EventCreated(self):
        #change these values to be suitable in your test environment

        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        self.MyTimer = self.CreateRexTimer(4,6)
        self.MyTimer.onTimer += self.HandleTimer
        
        self._testsRun = 0
        self._testsOK = 0

        print "botfunctiontest.Test EventCreated"

    def EventDestroyed(self):
        print "botfunctiontest.Test EventDestroyed"
        del self.MyTimer
        self.MyTimer = None

        super(self.__class__,self).EventDestroyed()

    def EventTouch(self,vAvatar):
        if(self.MyTimerCount > 0):
            self.llShout(0,"Test already running...")
            return
        self.MyAvatar = vAvatar
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

    def GetClosestBot(self,vSearchDistance):
        closestdist = LSLFloat(1000000)
        tempdist = 0
        foundavatar = None
        
        templist = self.MyAvatar.GetRadiusAvatars(vSearchDistance)
        for i in templist:
            tempavatar = self.MyAvatar.MyWorld.AllActors[i]
            if (tempavatar.IsHuman() == False):
                #self.log.Info("[PYTHON]: Found non human avatar")
                tempdist = self.MyAvatar.llVecMag(tempavatar.llGetPos()-self.MyAvatar.llGetPos())
                #self.log.InfoFormat("[PYTHON]: tempdist={0}, closestdist={1}", tempdist, closestdist)
                if(tempdist < closestdist):
                    closestdist = tempdist
                    foundavatar = tempavatar
                    #self.log.Info("[PYTHON]: Inserted to return")

        return foundavatar

    def IsApprox(self, firstPos, secondPos):
        if False == (firstPos.x+2 > secondPos.x and firstPos.x-2 < secondPos.x):
            return False
        if False == (firstPos.y+2 > secondPos.y and firstPos.y-2 < secondPos.y):
            return False
        
        #Don't check z axis. terrain can have height differences

        return True

    #BotWalkTo
    def Test1(self):
        vBot = self.GetClosestBot(100)
        self.ibotPos = vBot.llGetPos()
        newPos = Vector3(self.ibotPos.x+5, self.ibotPos.y, self.ibotPos.z)
        vBot.WalkTo(newPos)
        time.sleep(10) #wait 5 secs bot arrive to destination
        cPos = vBot.llGetPos()

        if self.IsApprox(cPos, newPos):
            self.llSay(0, "test 1 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 1 failed.")
            self.log.InfoFormat("[TEST1]: newpos={0}, destination={1}", cPos, newPos)

    #BotFlyTo
    def Test2(self):
        vBot = self.GetClosestBot(100)
        botPos = vBot.llGetPos()
        newPos = Vector3(botPos.x+5, botPos.y, botPos.z+10)
        vBot.FlyTo(newPos)
        time.sleep(10) #wait 5 secs bot arrive to destination
        cPos = vBot.llGetPos()

        if self.IsApprox(cPos, newPos):
            self.llSay(0, "test 2 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 2 failed.")
            self.log.InfoFormat("[TEST2]: newpos={0}, destination={1}", cPos, newPos)

    #BotRotateTo
    def Test3(self):
        vBot = self.GetClosestBot(100)
        vBot.llSetPos(self.ibotPos)
        vRot = vBot.llGetRot()
        vBot.RotateTo(Vector3(1,0,0))
        sRot = vBot.llGetRot()
        if vRot != sRot:
            self.llSay(0, "test 3 passed. old rot="+vRot.ToString()+" new rot="+sRot.ToString())
            self._testsOK += 1
        else:
            self.llSay(0, "test 3 failed.")


    #BotEnableAutoMove - test it although it is deprecated
    def Test4(self):
        vBot = self.GetClosestBot(100)
        vPos = vBot.llGetPos()
        vBot.EnableAutoMove(True)
        time.sleep(5)
        if vPos != vBot.llGetPos():
            self.llSay(0, "test 4 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 4 failed.")
        

    #BotPauseAutoMove
    def Test5(self):
        vBot = self.GetClosestBot(100)
        vPos = vBot.llGetPos()
        vBot.PauseAutoMove(False)
        time.sleep(5)
        cPos = vBot.llGetPos()
        if vPos == cPos:
            self.llSay(0, "test 5 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 5 failed.")
            self.log.InfoFormat("[TEST5]: newpos={0}, destination={1}", cPos, vPos)

    #BotStopAutoMove
    def Test6(self):
        vBot = self.GetClosestBot(100)
        vPos = vBot.llGetPos()
        vBot.StopAutoMove(False)
        time.sleep(5)
        cPos = vBot.llGetPos()
        if vPos == cPos:
            self.llSay(0, "test 6 passed")
            self._testsOK += 1
        else:
            self.llSay(0, "test 6 failed.")
            self.log.InfoFormat("[TEST6]: newpos={0}, destination={1}", cPos, vPos)
