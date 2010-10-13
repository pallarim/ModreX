import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3
List = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.list

import random
import math


class DialogActor(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampledialog.DialogActor"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "DialogActor EventCreated"
        self.llSetObjectName("QuestionPrim")
        self.llListen(1,"","","")

    def EventTouch(self, vAvatar):
        self.llDialog(vAvatar.AgentId, "Which do you like better?", List("Apples", "Oranges"),1)
        
    def listen(self,channel, name, id, message):
        if self.MyWorld.AllAvatars.has_key(id):
            avatar = self.MyWorld.AllAvatars[id]
            strmes = name + " selected option "  + message + ". Message channel:" + channel + " avatar full name:" + avatar.GetFullName()
            self.llShout(0,strmes)
        else:
            print "No avatar found mathing id:",id



        
        

