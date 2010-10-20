import rxactor
import rxavatar
import sys
import clr

import random
import math


class AO(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "sampleavataroverride.AO"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        print "sampleavataroverride.AO EventCreated"
        self.bOverride = False

    def EventTouch(self,vAvatar):
        self.bOverride = (not self.bOverride)
        
        if(self.bOverride):
            # Avatar storage url override
            # Get url from authentication server console when creating new avatar account and replace this sample url
            vAvatar.rexSetAvatarOverrideAddress("http://192.168.1.191:10000/avatar/4a7a7373fadd453ebc211a948338de43")
            self.llShout(0,"Overriding avatar storage url")
        else:
            vAvatar.rexSetAvatarOverrideAddress("")
            self.llShout(0,"Resetting avatar storage url override")
        
        
        





