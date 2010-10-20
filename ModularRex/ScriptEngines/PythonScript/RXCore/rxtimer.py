# rxtimer.py
# print "rxtimer.................................."
# Timer object.

import rxevent
import sys

class RexTimer(object):
    MyWorld = None

    def __init__(self, vTime=1.0,vCount=1):
        self.MyTriggerTime = 0
        self.MyTime = vTime
        self.MyMaxCount = vCount
        self.MyCurrentCount = 0
        self.onTimer = rxevent.RexPythonEvent()

        self.bProcessed = False
        self.bActive = False
        self.bStop = False

    def SetTimerValues(self,vTime,vCount):
        self.MyTime = vTime
        self.MyMaxCount = vCount
        self.MyCurrentCount = 0
        
    def Start(self):
        if(self.bProcessed):
            self.ResetTimer()
            return
        
        if(self.bActive):
            self.Stop()

        RexTimer.MyWorld.MyEventManager.StartTimer(self)
        self.bActive = True

    def Stop(self):
        if(self.bProcessed):
            self.bStop = True
            return
        
        if(not self.bActive):
            return
        
        RexTimer.MyWorld.MyEventManager.StopTimer(self)
        self.TimerFinished()
        
    def ResetTimer(self):
        self.MyCurrentCount = 0
        
    def DoTimerEvent(self):
        self.bProcessed = True
        self.MyCurrentCount += 1
        try:
            self.onTimer()
        except:
            print "rxtimer,DoTimerEvent", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]
            
        self.bProcessed = False

        if(self.bStop):
            self.bStop = False
            return True
        elif((self.MyMaxCount > 0) and (self.MyCurrentCount >= self.MyMaxCount)):
            return True
        else:
            return False

    def __cmp__(self, other):
        return (self.MyTriggerTime - other.MyTriggerTime)
    
    def TimerFinished(self):
        self.bActive = False


        
