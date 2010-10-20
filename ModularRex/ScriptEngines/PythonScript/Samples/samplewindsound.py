import rxactor
import rxavatar
import sys
import clr

asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')

class WindSound(rxactor.Actor):
	@staticmethod
	def GetScriptClassName():
		return "samplewindsound.WindSound"

	def EventCreated(self):
		super(self.__class__,self).EventCreated()
		print "samplewindsound.WindSound EventCreated"
		self.Status = 0
		
	def EventTouch(self,vAvatar):
		if(self.Status == 0):
			self.llShout(0, "Wind sound disabled")
			vAvatar.rexToggleWindSnd(True)
		else:
			self.llShout(0, "Wind sound enabled")
			vAvatar.rexToggleWindSnd(False)

		self.Status += 1
		
		if(self.Status > 1):
			self.Status = 0
		
		
