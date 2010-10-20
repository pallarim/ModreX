# rxeventmanager.py
# print "rxeventmanager.................................."

import sys
import rxactor
import rxevent
import rxworld
import rxavatar
import rxbot
import rxtimer

import bisect

class EventManager(object):
    def __init__(self,vWorld):
        super(self.__class__,self).__init__()
        self.MyWorld = vWorld
        self.MyWorld.MyEventManager = self
        rxtimer.RexTimer.MyWorld = vWorld
        
        self.MyActiveListIndex = 0
        self.MyTimerList = []
        self.MyRexTimerList = []
        self.CurrTime = 0
        self.MyEventListA = []
        self.MyEventListB = []
        self.bShutDown = False

        # Register events
        self.MyEventClasses = {}
        self.MyEventClasses[rxevent.RexEventTouchStart.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventTouchStart.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventTouchStart.MyName)
        self.MyEventClasses[rxevent.RexEventSetTimer.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventSetTimer.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventSetTimer.MyName)
        self.MyEventClasses[rxevent.RexEventAddEntity.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventAddEntity.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventAddEntity.MyName)
        self.MyEventClasses[rxevent.RexEventRemoveEntity.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventRemoveEntity.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventRemoveEntity.MyName)
        self.MyEventClasses[rxevent.RexEventAddPresence.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventAddPresence.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventAddPresence.MyName)
        self.MyEventClasses[rxevent.RexEventRemovePresence.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventRemovePresence.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventRemovePresence.MyName)
        self.MyEventClasses[rxevent.RexEventClientEvent.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventClientEvent.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventClientEvent.MyName)
        self.MyEventClasses[rxevent.RexEventPrimVolumeCollision.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventPrimVolumeCollision.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventPrimVolumeCollision.MyName)
        self.MyEventClasses[rxevent.RexEventListen.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventListen.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventListen.MyName)
        self.MyEventClasses[rxevent.RexEventClientStartup.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventClientStartup.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventClientStartup.MyName)
        self.MyEventClasses[rxevent.RexEventAddBot.MyName] = getattr(self,"CreateEvent_"+rxevent.RexEventAddBot.MyName),getattr(self,"HandleEvent_"+rxevent.RexEventAddBot.MyName)
        # RexEventTimer events are in their own list, so not added here.

        # Internal python events that all actors can subscribe to c-sharp style
        self.onAddEntity = rxevent.RexPythonEvent()
        self.onRemoveEntity = rxevent.RexPythonEvent()
        self.onAddPresence = rxevent.RexPythonEvent()
        self.onRemovePresence = rxevent.RexPythonEvent()
        self.onMouseLeft = rxevent.RexPythonEvent()
        self.onMouseRight = rxevent.RexPythonEvent()
        self.onMouseWheel = rxevent.RexPythonEvent()
        self.onTick = rxevent.RexPythonEvent()
        self.onClientStartup = rxevent.RexPythonEvent()
        self.onAddBot = rxevent.RexPythonEvent()
        self.onRemoveBot = rxevent.RexPythonEvent()

    def ShutDown(self):
        try:
            self.bShutDown = True
            
            # Registered events
            self.MyEventClasses.clear()
            
            # Delete event lists
            #print "deleting MyEventListA A"
            while len(self.MyEventListA) > 0:
                TempEvent = self.MyEventListA.pop(0)
                del TempEvent

            #print "deleting MyEventListA B"
            while len(self.MyEventListB) > 0:
                TempEvent = self.MyEventListB.pop(0)
                del TempEvent

            # Delete timer event list
            #print "deleting Timerlist"
            while len(self.MyTimerList) > 0:
                TempEvent = self.MyTimerList.pop(0)
                del TempEvent
            
            # TickedActor list, clear list only
            #print "deleting MyTickedActors"
            
            #if len(self.MyTickedActors) > 0:
            #    del self.MyTickedActors[:]
            #while len(self.MyTickedActors) > 0:
            #    TempActor = self.MyTickedActors.pop(0)
            
            # Avatar list, clear list only
            #print "deleting AllAvatars"
            while len(self.MyWorld.AllAvatars) > 0:
                TempKey = self.MyWorld.AllAvatars.keys()[0]
                TempActor = self.MyWorld.AllAvatars.pop(TempKey)
                del TempActor
            #while len(self.MyWorld.AllAvatars) > 0:
            #    TempActor = self.MyWorld.AllAvatars.pop()

            # Actor list
            # print "deleting AllActors"
            while len(self.MyWorld.AllActors) > 0:
                TempKey = self.MyWorld.AllActors.keys()[0]
                self.DeleteActor(TempKey)

            # print "Settings lists to none"
            self.MyEventClasses = None
            self.MyEventListA = None
            self.MyEventListB = None
            self.MyTimerList = None
            self.MyRexTimerList = None
            
            # Fixme, empty lists before Noneing them?
            self.onAddEntity = None
            self.onRemoveEntity = None
            self.onAddPresence = None
            self.onRemovePresence = None
            self.onMouseLeft = None
            self.onMouseRight = None
            self.onMouseWheel = None
            self.onTick = None
            self.onClientStartup = None
            self.onAddBot = None
            self.onRemoveBot = None
        except:
            print "EventManager,shutDown", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]
        




    def CreateEventWithName(self,vName,*args):
        try:
            if self.bShutDown:
                return;
            
            TempEvent = None
            
            if(self.MyEventClasses.has_key(vName)):
                eventfuncdata = self.MyEventClasses[vName]
                TempEvent = eventfuncdata[0](*args)
            else:
                print "rxeventmanager,CreateEventWithName, no event named",vName

            if TempEvent != None:
                if self.MyActiveListIndex == 0:
                    self.MyEventListB.append(TempEvent);
                else:
                    self.MyEventListA.append(TempEvent);
        except:
            print "rxeventmanager,EventManager,CreateEventWithName", sys.exc_info()[0]
            print sys.exc_info()[1]
            print sys.exc_info()[2]



    def SwitchActiveList(self):
        if self.MyActiveListIndex == 0:
            self.MyActiveListIndex = 1
        else:
            self.MyActiveListIndex = 0


    def ProcessEvents(self,vDeltaTime):
        while True:
            try:
                TempEvent = None
                
                if self.MyActiveListIndex == 0:
                    if len(self.MyEventListA) == 0:
                        break
                    else:
                        TempEvent = self.MyEventListA.pop(0)
                else:
                    if len(self.MyEventListB) == 0:
                        break
                    else:
                        TempEvent = self.MyEventListB.pop(0)

                # What to do with events
                if(self.MyEventClasses.has_key(TempEvent.MyName)):
                    eventfuncdata = self.MyEventClasses[TempEvent.MyName]
                    eventfuncdata[1](TempEvent)
                else:
                    print "Unknown event not processed",TempEvent.MyName

                del TempEvent
            except:
                print "rxeventmanager,ProcessEvents:", sys.exc_info()[0]
                print sys.exc_info()[1]
                print sys.exc_info()[2]

    def CallTimeEvents(self,vDeltaTime):
        try:
            # Tick
            try:
                self.onTick(vDeltaTime)
            except:
                print "rxeventmanager,CallTimeEvents,tick:", sys.exc_info()[0]
                print sys.exc_info()[1]
                print sys.exc_info()[2]

            # Timer
            while len(self.MyTimerList) > 0:
                TObj = self.MyTimerList[0]
                TempActor = None

                if TObj.TTime <= self.CurrTime:
                    TObj = self.MyTimerList.pop(0)
                    if self.MyWorld.AllActors.has_key(TObj.ObjectId):
                        try:
                            TempActor = self.MyWorld.AllActors[TObj.ObjectId]
                            TempActor.EventTimer()
                        except:
                            print "rxeventmanager,CallTimeEvents,timer:", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]
                    else:
                        print "No Actor for timer event",TObj.ObjectId

                    if TempActor != None:
                        if TempActor.bTimerLoop and TempActor.MyTimerCount > 0:
                            self.CreateTimerEventForLooping(TempActor)
                            #self.SetTimerForActor(TempActor,TempActor.MyTimerCount,TempActor.bTimerLoop)
                        else:
                            TempActor.MyTimerCount = 0
                            TempActor.bTimerLoop = False
                    del TObj
                else:
                    break
                
                
            # RexTimer
            bRemove = False
            currextimer = None
            while(len(self.MyRexTimerList) > 0):
                if (self.MyRexTimerList[0].MyTriggerTime <= self.CurrTime):
                    bRemove = False
                    currextimer = self.MyRexTimerList.pop(0)
                    bRemove = currextimer.DoTimerEvent()
                    if(not bRemove):
                        self.StartTimer(currextimer)
                    else:
                        currextimer.TimerFinished()
                else:
                    break
        except:
            print "CallTimeEvents", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateTimerEventForLooping(self,vActor):
        try:
            TempObj = rxevent.RexEventTimer(vActor.Id,self.CurrTime + vActor.MyTimerCount,vActor.bTimerLoop)

            index = 0
            for i in self.MyTimerList:
                if TempObj.TTime < i.TTime:
                    self.MyTimerList.insert(index,TempObj)
                    del TempObj
                    return
                else:
                    index += 1

            # Insert as last
            self.MyTimerList.append(TempObj)
            del TempObj
        except:
            print "CreateTimerEventForLooping", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]



    
    # Handle events
    # **********************************************************
    def CreateEvent_touch_start(self,*args):
        return rxevent.RexEventTouchStart(*args)

    def HandleEvent_touch_start(self,vEvent):
        try:
            if self.MyWorld.AllActors.has_key(vEvent.ObjectId):
                TempActor = self.MyWorld.AllActors[vEvent.ObjectId]

                if self.MyWorld.AllAvatars.has_key(vEvent.AgentId):
                    TempActor.EventTouch(self.MyWorld.AllAvatars[vEvent.AgentId])
                else:
                    print "MISSING AVATAR FOR TOUCH",vEvent.AgentId
            else:
                print "TOUCH EVENT FOR MISSING ACTOR",vEvent.ObjectId,vEvent.AgentId
        except:
            print "HandleEvent_TouchStart", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    # Avatar
    # *****************************************
    def CreateEvent_add_presence(self,*args):
        return rxevent.RexEventAddPresence(*args)

    def HandleEvent_add_presence(self,vEvent):
        try:
            # If already on list, leave there.
            if self.MyWorld.AllAvatars.has_key(vEvent.ObjectId):
                print "HandleEvent_AddPresence, avatar is already on avatarlist",vEvent.AgentId
                return

            TempAvatar = rxavatar.Avatar(vEvent.ObjectId)
            TempAvatar.AgentId = vEvent.AgentId
            
            self.MyWorld.AllAvatars[vEvent.AgentId] = TempAvatar
            self.AddActor(TempAvatar)
            self.onAddPresence(TempAvatar)
        except:
            print "HandleEvent_AddPresence", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateEvent_remove_presence(self,*args):
        return rxevent.RexEventRemovePresence(*args)
    
    def HandleEvent_remove_presence(self,vEvent):
        try:
            if self.MyWorld.AllAvatars.has_key(vEvent.AgentId):
                TempAgent = self.MyWorld.AllAvatars.pop(vEvent.AgentId)
                
                if isinstance(TempAgent,rxbot.Bot):
                    self.onRemoveBot(TempAgent)
                else:
                    self.onRemovePresence(TempAgent)

                AgentObjId = TempAgent.Id
                del TempAgent
                self.DeleteActor(AgentObjId)
            else:
                print "HandleEvent_RemovePresence, avatar not on avatarlist",vEvent.AgentId
        except:
            print "HandleEvent_RemovePresence", sys.exc_info()[0]
            print sys.exc_info()[1]
            print sys.exc_info()[2]


    # Bot
    # ****************************************************
    def CreateEvent_add_bot(self,*args):
        return rxevent.RexEventAddBot(*args)

    def HandleEvent_add_bot(self,vEvent):
        try:
            if self.MyWorld.AllAvatars.has_key(vEvent.ObjectId):
                print "HandleEvent_add_bot, avatar is already on avatarlist",vEvent.AgentId
                return

            TempAvatar = rxbot.Bot(vEvent.ObjectId)
            TempAvatar.AgentId = vEvent.AgentId

            self.MyWorld.AllAvatars[vEvent.AgentId] = TempAvatar
            self.AddActor(TempAvatar)
            self.onAddBot(TempAvatar)
        except:
            print "HandleEvent_add_bot", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]

    # Prim
    def CreateEvent_add_entity(self,*args):
        return rxevent.RexEventAddEntity(*args)

    def HandleEvent_add_entity(self,vEvent):
        try:
            if self.MyWorld.AllActors.has_key(str(vEvent.ObjectId)):
                TempActor = self.MyWorld.AllActors[str(vEvent.ObjectId)]
                TempActor.EventPreCreated()
                TempActor.EventCreated()
                self.onAddEntity(TempActor)
            else:
                print "HandleEvent_AddEntity trying to be used the old way, actor not yet created."
        except:
            print "HandleEvent_AddEntity", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]



    def CreateEvent_remove_entity(self,*args):
        return rxevent.RexEventRemoveEntity(*args)
    
    def HandleEvent_remove_entity(self,vEvent):
        try:
            if not self.DeleteActor(vEvent.ObjectId):
                print "HandleEvent_RemoveEntity, no entity found with id:",vEvent.ObjectId
        except:
            print "HandleEvent_RemoveEntity", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    # Timer
    # **************************************************
    def CreateEvent_set_timer(self,*args):
        return rxevent.RexEventSetTimer(*args)

    def HandleEvent_set_timer(self,vEvent):
        try:
            # If has previous time on list, remove it!
            for i in self.MyTimerList:
                if i.ObjectId == vEvent.ObjectId:
                    TempObj = self.MyTimerList.remove(i)
                    del TempObj
                    break

            if not self.MyWorld.AllActors.has_key(vEvent.ObjectId):
                #print "HandleEvent_SetTimer, actor not found ",vEvent.ObjectId
                return

            TempActor = self.MyWorld.AllActors[vEvent.ObjectId]
            TempActor.MyTimerCount = vEvent.TTime
            TempActor.bTimerLoop = vEvent.bLoop

            if vEvent.TTime <= 0:
                return;

            TempObj = rxevent.RexEventTimer(vEvent.ObjectId,self.CurrTime + vEvent.TTime,vEvent.bLoop)

            index = 0
            for i in self.MyTimerList:
                if TempObj.TTime < i.TTime:
                    self.MyTimerList.insert(index,TempObj)
                    del TempObj
                    return
                else:
                    index += 1

            # Insert as last
            self.MyTimerList.append(TempObj)
            del TempObj
        except:
            print "HandleEvent_SetTimer", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateEvent_client_event(self,*args):
        return rxevent.RexEventClientEvent(*args)

    # Client event which was sent by client
    # param 0 is avatarid (lluuid)
    # param 1 is command (str)
    def HandleEvent_client_event(self,vEvent):
        try:
            if self.MyWorld.AllAvatars.has_key(vEvent.AgentId):
                TempAgent = self.MyWorld.AllAvatars[vEvent.AgentId]
            else:
                print "HandleEvent_ClientEvent, avatar not on avatarlist",vEvent.AgentId
                return

            command = str(vEvent.Params[1])
            # Left mouse button pressed
            if(command == "lmb"):
                self.onMouseLeft(TempAgent)
            # Right mouse button pressed
            elif(command == "rmb"):
                self.onMouseRight(TempAgent)
            # Mouse wheel
            elif(command == "mw"):
                self.onMouseWheel(TempAgent,str(vEvent.Params[2]))
            else:
                print "HandleEvent_ClientEvent, unhandled command:",command + " from " + TempAgent.GetFullName()
        except:
            print "HandleEvent_ClientEvent", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateEvent_primvol_col(self,*args):
        return rxevent.RexEventPrimVolumeCollision(*args)

    def HandleEvent_primvol_col(self,vEvent):
        try:
            if self.MyWorld.AllActors.has_key(vEvent.ObjectId):
                TempActor = self.MyWorld.AllActors[vEvent.ObjectId]

                if self.MyWorld.AllActors.has_key(vEvent.ColliderId):
                    TempActor.EventPrimVolumeCollision(self.MyWorld.AllActors[vEvent.ColliderId])
                else:
                    print "MISSING ACTOR FOR PRIMVOLUMECOLLISION",vEvent.ColliderId
            else:
                print "PRIMVOLUMECOLLISION EVENT FOR MISSING ACTOR",vEvent.ObjectId,vEvent.ColliderId
        except:
            print "HandleEvent_PrimVolumeCollision", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateEvent_listen(self,*args):
        return rxevent.RexEventListen(*args)

    def HandleEvent_listen(self,vEvent):
        try:
            if self.MyWorld.AllActors.has_key(vEvent.ObjectId):
                #print "All good so far"
                TempActor = self.MyWorld.AllActors[vEvent.ObjectId]
                #print "Still good"
                #print "Channel: " + vEvent.Channel
                #print " type: " + str(vEvent.Channel.__class__)
                #print "EvName: " + vEvent.EvName
                #print " type: " + str(vEvent.EvName.__class__)
                #print "OtherId: " + vEvent.OtherId
                #print " type: " + str(vEvent.OtherId.__class__)
                #print "Message: " + vEvent.Message
                #print " type: " + str(vEvent.Message.__class__)
                TempActor.listen(vEvent.Channel,vEvent.EvName,vEvent.OtherId,vEvent.Message)
            else:
                print "LISTEN EVENT FOR MISSING ACTOR",vEvent.ObjectId
        except:
            print "HandleEvent_listen", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]


    def CreateEvent_client_startup(self,*args):
        return rxevent.RexEventClientStartup(*args)

    def HandleEvent_client_startup(self,vEvent):
        try:
            if self.MyWorld.AllAvatars.has_key(vEvent.AgentId):
                TempAgent = self.MyWorld.AllAvatars[vEvent.AgentId]
                self.onClientStartup(TempAgent,vEvent.Status)
            else:
                print "CLIENTSTARTUP EVENT FOR MISSING AVATAR",vEvent.AgentId
        except:
            print "HandleEvent_client_startup", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]









    def AddActor(self,vActor):
        try:
            if self.MyWorld.AllActors.has_key(vActor.Id):
                print "AddActor, actor already on list",vActor.Id
                return False

            vActor.MyWorld = self.MyWorld
            self.MyWorld.AllActors[vActor.Id] = vActor
            vActor.EventPreCreated()
            vActor.EventCreated()
            return True
        except:
            print "AddActor", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]
        

    def DeleteActor(self,vId):
        try:
            if self.MyWorld.AllActors.has_key(vId):
                self.onRemoveEntity(self.MyWorld.AllActors[vId])
                TempActor = self.MyWorld.AllActors.pop(vId)
                TempActor.SetTimer(0,False)

                # If actor has subscribed to any events, it should unsubscribe itself
                TempActor.EventDestroyed()
                del TempActor
                return True
            else:
                print "DeleteActor, no actor found with id:",vId
                return False
        except:
            print "DeleteActor", sys.exc_info()[0],sys.exc_info()[1],sys.exc_info()[2]
            return False


    def EnableTickForActor(self,vActor):
        self.onTick += vActor.EventTick
        
    def DisableTickForActor(self,vActor):
        self.onTick -= vActor.EventTick

    def SetTimerForActor(self,vActor,vTime,vbLoop):
        self.CreateEventWithName("set_timer",vActor.Id,vTime,vbLoop)

    def PrintActorList(self):
        print "Printing actor list..."
        for iid, iactor in self.MyWorld.AllActors.iteritems():
            print iid,iactor



    # New timer handling
    # *************************************************************
    def StartTimer(self,vRexTimer):
        vRexTimer.MyTriggerTime = (vRexTimer.MyTime + self.CurrTime)
        bisect.insort(self.MyRexTimerList,vRexTimer)
    
    def StopTimer(self,vRexTimer):
        tempindex = bisect.bisect_left(self.MyRexTimerList,vRexTimer)
        endindex = tempindex+100
        while(tempindex < endindex):
            if(tempindex >= len(self.MyRexTimerList)):
                print "Unable to stop rextimer, end reached",vRexTimer
                return
            
            if(id(self.MyRexTimerList[tempindex]) == id(vRexTimer)):
                self.MyRexTimerList.pop(tempindex)
                return
            else:
                tempindex +=1
                
        print "Unable to stop rextimer",vRexTimer
