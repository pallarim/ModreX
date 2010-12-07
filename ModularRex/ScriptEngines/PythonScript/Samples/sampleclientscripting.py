# sampleclientscripting.py
#print "sampleclientscripting.................................."

import rxactor
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3

import random
import math

# Commands available on client-hud:
#  ShowInventoryMessage(vMessage)
#  ShowScrollMessage(vMessage,vTime)
#  ShowTutorialBox(vText,vTime):
#  DoFadeInOut(vInTime, vBetweenTime,vOutTime)
    
# Commands available on client-client:
#  mousebtns, 0=off, 1=on


# All effects in one class.
class ClientScripting(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclientscripting.ClientScripting"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "ClientScripting EventCreated"
        self.SendItem = 0

    def EventTouch(self,vAvatar):
        if(self.SendItem == 0):
            vAvatar.ShowInventoryMessage("This is a message from server")
            str = self.llGetObjectName() +  " sent ShowInventoryMessage command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 1):
            vAvatar.ShowInventoryMessageAdv("This is advanced inventory message for 15 secs",15,0.9,0.2,0.2,0.2)
            str = self.llGetObjectName() +  " sent ShowInventoryMessageAdv command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 2):
            vAvatar.ShowInventoryMessageAdv("This is advanced inventory message for 10 secs",10,0.1,1,0.1,1)
            str = self.llGetObjectName() +  " sent ShowInventoryMessageAdv command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 3):
            vAvatar.ShowScrollMessage("This is a scrolling message from server lasting 10 seconds",10)
            str = self.llGetObjectName() +  " sent ShowScrollMessage command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 4):
            vAvatar.ShowTutorialBox("This is a tutorial message box from server lasting 10 seconds",10)
            str = self.llGetObjectName() +  " sent ShowTutorialBox command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 5):
            vAvatar.DoFadeInOut(3,3,3)
            str = self.llGetObjectName() +  " sent DoFadeInOut command to client " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 6):
            self.MyWorld.MyEventManager.onMouseLeft += self.handleOnMouseLeft
            self.MyWorld.MyEventManager.onMouseRight += self.handleOnMouseRight
            vAvatar.SetSendMouseClickEvents(True)
            str = self.llGetObjectName() +  " sent client-enablesendmousebtns " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 7):
            self.MyWorld.MyEventManager.onMouseLeft -= self.handleOnMouseLeft
            self.MyWorld.MyEventManager.onMouseRight -= self.handleOnMouseRight
            vAvatar.SetSendMouseClickEvents(False)
            str = self.llGetObjectName() +  " sent client-disablesendmousebtns " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 8):
            self.MyWorld.MyEventManager.onMouseWheel += self.handleOnMouseWheel
            vAvatar.SetSendMouseWheelEvents(True)
            str = self.llGetObjectName() +  " sent client-enablesendmousewheel " + vAvatar.GetFullName()
            self.llShout(0,str)
        elif (self.SendItem == 9):
            self.MyWorld.MyEventManager.onMouseWheel -= self.handleOnMouseWheel
            vAvatar.SetSendMouseWheelEvents(False)
            str = self.llGetObjectName() +  " sent client-disablesendmousewheel " + vAvatar.GetFullName()
            self.llShout(0,str)
        
        self.SendItem = self.SendItem+1
        if(self.SendItem > 9):
            self.SendItem = 0


    def handleOnMouseLeft(self,vAgent):
        str = "Left mouse button was pressed by " + vAgent.GetFullName()
        self.llShout(0,str)

    def handleOnMouseRight(self,vAgent):
        str = "Right mouse button was pressed by " + vAgent.GetFullName()
        self.llShout(0,str)

    def handleOnMouseWheel(self,vAgent,vAction):
        if(vAction == "-1"):
            str = "Mouse wheel up by" + vAgent.GetFullName()
        else:
            str = "Mouse wheel down by" + vAgent.GetFullName()
        self.llShout(0,str)
        
        
        
        
# Senses, blind
class BlindTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclientscripting.BlindTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "BlindTest EventCreated"
        self.SendItem = 0
        self.BlindType = 0

    def EventTouch(self,vAvatar):
        vAvatar.SetBlindness(self.BlindType,self.SendItem)
        mytext = "SetBlindness set to " + str(self.SendItem)
        self.llShout(0,mytext)

        self.SendItem = self.SendItem+10
        if(self.SendItem > 100):
            self.BlindType = self.BlindType+1
            self.SendItem = 0
            if(self.BlindType > 2):
                self.BlindType = 0

# Senses, deaf
class DeafTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclientscripting.DeafTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "DeafTest EventCreated"
        self.DeafValue = False

    def EventTouch(self,vAvatar):
        self.DeafValue = (not self.DeafValue)
        vAvatar.SetDeaf(self.DeafValue)
        mes = "Deaf set to " + str(self.DeafValue) + " " + vAvatar.GetFullName()
        self.llShout(0,mes)

# Senses, mute
class MuteTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclientscripting.MuteTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "MuteTest EventCreated"
        self.MuteValue = False

    def EventTouch(self,vAvatar):
        self.MuteValue = (not self.MuteValue)
        vAvatar.SetMute(self.MuteValue)
        mes = "Mute set to " + str(self.MuteValue) + " " + vAvatar.GetFullName()
        self.llShout(0,mes)

        
# More info about an object by touching it.
class ArmChair(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleclientscripting.ArmChair"
    
    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "ClientScripting.ArmChair EventCreated"
        self.Status = 0

    def EventTouch(self,vAvatar):
        if(self.Status == 0):
            self.CommandToClient(vAvatar.AgentId,'hud','ShowTutorialBox("RXR armchair for sale by rexuser, click again for more info)",6)','')
        else:
            self.CommandToClient(vAvatar.AgentId,'hud','ShowScrollMessage("RXR chair is very comfy to sit on. It features ultra modern fibers making sitting in it a comfortable experience. You will never sit in another chair again.",30)','')

        self.Status = self.Status+1
        if(self.Status > 1):
            self.Status = 0
        

