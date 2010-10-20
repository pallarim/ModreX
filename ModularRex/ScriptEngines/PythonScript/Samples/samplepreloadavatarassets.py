import rxactor
import sys
import math
import clr

from System.Collections.Generic import List as GenericList

class PreLoadAvatarAssets(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplepreloadavatarassets.PreLoadAvatarAssets"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplepreloadavatarassets.PreLoadAvatarAssets EventCreated"

    def EventTouch(self,vAvatar):
        assetlist = GenericList[str]()
        assetlist.Add("http://192.168.1.11:10000/avatar/4d594979e0614c0ebe7d3cff190eccf3") # shark
        assetlist.Add("http://192.168.1.175:10000/avatar/1296a73c9ce1405996130f51df458ba2") # some fish
        vAvatar.rexPreloadAvatarAssets(assetlist)
        self.llShout(0,"Preload avatar assets command sent to client")

