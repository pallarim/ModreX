import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math


class Test(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "testobject.Test"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.TickCount = 0
        self.MyTimer = self.CreateRexTimer(4,18)
        self.MyTimer.onTimer += self.HandleTimer
        
        print "testobject.Test EventCreated"

    def EventDestroyed(self):
        print "testobject.Test EventDestroyed"
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

    # Basic tests
    def Test1(self):
        self.llShout(0,"Test1,scale")
        scalex = 0.5 + random.random()*2
        scaley = 0.5 + random.random()*2
        scalez = 0.5 + random.random()*2
        tempscale = Vector3(scalex,scaley,scalez)
        self.llSetScale(tempscale)
        
    def Test2(self):
        self.llShout(0,"Test2,settext")
        self.llSetText("On top of text",Vector3(1,0,0),1)
        
    def Test3(self):
        self.llShout(0,"Test3,setrot")
        rotx = 0.5 + random.random()*3.14
        roty = 0.5 + random.random()*3.14
        rotz = 0.5 + random.random()*3.14
        r = self.llEuler2Rot(Vector3(rotx,roty,rotz))
        self.llSetRot(r)
        
    def Test4(self):
        self.llShout(0,"Test4,setloc")
        loc = self.llGetPos() + Vector3(0,0,1)
        self.llSetPos(loc)

    def Test5(self):
        self.llShout(0,"Test5,spin with llTargetOmega")
        self.llTargetOmega(Vector3(1,0,5),3.14,1.0)

    def Test6(self):
        self.llShout(0,"Test6,rez new prim")
        self.llTargetOmega(Vector3(0,0,0),0,0)
        spawnloc = self.llGetPos() + Vector3(0,0,2)
        self.MySpawnedId = self.SpawnActor(spawnloc,0,False,"testobject.Test")

    def Test7(self):
        self.llShout(0,"Test7,rezzed prim scale")
        tempprim = self.MyWorld.AllActors[self.MySpawnedId]
        tempprim.llSetScale(Vector3(2,2,2))

    def Test8(self):
        self.llShout(0,"Test8,setmesh")
        tempprim = self.MyWorld.AllActors[self.MySpawnedId]
        tempprim.SetRexDrawType(1)
        tempprim.SetRexMeshByName("rock")

    def Test9(self):
        self.llShout(0,"Test9,setmaterial")
        tempprim = self.MyWorld.AllActors[self.MySpawnedId]
        tempprim.RexSetMaterialByName(0,"graniteblock")

    def Test10(self):
        self.llShout(0,"Test10,DestroyActor")
        tempprim = self.MyWorld.AllActors[self.MySpawnedId]
        tempprim.DestroyActor()

    def Test11(self):
        # Enable tick, disabled in next test
        self.llShout(0,"Test11,Tick enabled")
        self.EnableTick()

    # Avatar tests
    def Test12(self):
        self.DisableTick()
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]
        self.llShout(0,"Test12,rot to north "+tempavatar.GetFullName())
        rotresult = self.llEuler2Rot(Vector3(0,0,math.pi*0.5))
        tempavatar.llSetRot(rotresult)
        
    def Test13(self):
        self.llShout(0,"Test13,towards me rotation")
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]
        avatar_forward = self.llRot2Fwd(tempavatar.llGetRot())
        avatar_toward = self.llVecNorm(self.llGetPos() - tempavatar.llGetPos())
        rotresult = self.llRotBetween(avatar_forward,avatar_toward)
        tempavatar.SetRelativeRot(rotresult)

    def Test14(self):
        self.llShout(0,"Test14,localteleport")
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]
        Offset = self.llVecNorm(self.llGetPos() - tempavatar.llGetPos())
        tempavatar.DoLocalTeleport(tempavatar.llGetPos()+Offset*2)

    def Test15(self):
        self.llShout(0,"Test15,movementspeed")
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]
        tempavatar.SetMovementModifier(1.75)

    def Test16(self):
        self.llShout(0,"Test16,showinventorymessage")
        tempavatar = self.MyWorld.AllAvatars[self.AgentId]
        tempavatar.SetMovementModifier(1)
        tempavatar.ShowInventoryMessage("Can you see this?")
        
    def Test17(self):
        self.llShout(0,"Test17,setting PrimFreeData to sampledata=testing123")
        self.PrimFreeData = "sampledata=testing123"
        
    def Test18(self):
        self.llShout(0,"Test18,RexFreePrimData from server:"+self.PrimFreeData)
        

