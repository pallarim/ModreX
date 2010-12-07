import rxX10
import clr
asm = clr.LoadAssemblyByName('OpenSim.Region.ScriptEngine.Shared')
Vector3 = asm.OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3
    
# A simple lamp object
# - Turns on/off when touched
# - object color changes  
class Lamp(rxX10.X10Actor):

    def __init__(self,vId):
        super(Lamp,self).__init__(vId)
        self.state = 1

    @staticmethod
    def GetScriptClassName():
        return "sampleX10.Lamp"

    # The SWITCH logic
    def EventTouch(self,vAvatar):
        self.state = ( self.state + 1 ) % 2    # change state
        if self.state:
            self.TurnOn()
        else:
            self.TurnOff()

    # Turn ON Action            
    def TurnOnAction(self):
        self.state = 1
        val = 1.0
        color = Vector3(val ,val ,val )
        self.llSetColor(color, -1 )

    # Turn OFF Action            
    def TurnOffAction(self):
        self.state = 0
        val = 0.5
        color = Vector3(val ,val ,val )
        self.llSetColor(color, -1 )
        
                
