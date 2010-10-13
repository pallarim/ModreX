import rxactor
import sys
import math
import clr

from System.Collections.Generic import List as GenericList

class PreLoadAssets(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "samplepreloadassets.PreLoadAssets"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "samplepreloadssets.PreLoadAssets EventCreated"

    def EventTouch(self,vAvatar):
        assetlist = GenericList[str]()
        assetlist.Add("00000000-0000-2222-3333-100000001028") # Rockwallbig
        assetlist.Add("00000000-0000-2222-3333-100000001034") # Seawater
        assetlist.Add("00000000-0000-2222-3333-100000001040") # Thatch
        vAvatar.rexPreloadAssets(assetlist)
        self.llShout(0,"Preload command sent to client")



