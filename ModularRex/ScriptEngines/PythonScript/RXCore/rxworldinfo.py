# rxworldinfo.py
# Parent class for worldinfo objects
# print "rxworldinfo.................................."

import rxactor

class WorldInfo(rxactor.Actor):
    
    @staticmethod
    def GetScriptClassName():
        return "rxworldinfo.WorldInfo"
    
    def GetAvatarStartLocation(self):
        return None
        
