# rxbot.py
#print "rxbot.................................."

import sys
import clr
import rxavatar

class Bot(rxavatar.Avatar):
    @staticmethod
    def GetScriptClassName():
        return "rxbot.Bot"
    
    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.Controller = None
        # print "Bot EventCreated",self.Id
        pass

    def EventDestroyed(self):
        super(self.__class__,self).EventDestroyed()
        # print "Bot EventDestroyed",self.Id
        pass

    def IsHuman(self):
        return False
    def IsBot(self):
        return True

    def WalkTo(self,vDest):
        self.MyWorld.CS.BotWalkTo(self.AgentId,vDest)

    def FlyTo(self,vDest):
        self.MyWorld.CS.BotFlyTo(self.AgentId,vDest)

    def RotateTo(self,vDest):
        self.MyWorld.CS.BotRotateTo(self.AgentId,vDest)

    # deprecated, prefer PauseAutoMove() and StopAutoMove()
    def EnableAutoMove(self,vEnable):
        self.MyWorld.CS.BotEnableAutoMove(self.AgentId,vEnable)

    def PauseAutoMove(self,vEnable):
        self.MyWorld.CS.BotPauseAutoMove(self.AgentId,vEnable)
        
    def StopAutoMove(self,vEnable):
        self.MyWorld.CS.BotStopAutoMove(self.AgentId,vEnable)
        
    def SendChat(self, message):
        self.MyWorld.CS.BotSendChatMessage(self.AgentId, message)
        
