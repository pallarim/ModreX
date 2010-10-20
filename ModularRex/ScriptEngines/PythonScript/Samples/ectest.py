import rxactor
import sys
import clr

from System.Collections.Generic import Dictionary

class ECTest(rxactor.Actor):
    @staticmethod
    def GetScriptClassName():
        return "ectest.ECTest"

    def EventCreated(self):
        super(self.__class__,self).EventCreated()
        self.lightenabled = True
        print "ECTest created"

    def EventTouch(self,vAvatar):
        print "Toggling EC_Light diffuse color attribute"

        # Get existing attrs (they're in a C# Dictionary)
        attrs = self.rexGetECAttributes("EC_Light")

        # Change diffuse color. Note: all manipulation so far happens with strings
        if self.lightenabled == True:
            attrs["diffuse color"] = "1 1 1 1"
            self.lightenabled = False
        else:
            attrs["diffuse color"] = "0 0 0 1"
            self.lightenabled = True

        # Set changed attrs
        self.rexSetECAttributes(attrs, "EC_Light")





        
        
