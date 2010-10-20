import rxactor
import rxavatar
import sys
import clr
from BaseHTTPServer import BaseHTTPRequestHandler, HTTPServer
import urllib
import select
from threading import Thread
import time

# Listen one http port and forward incoming X10 messages all X10 objects
class HttpRequestHandler(BaseHTTPRequestHandler):

    x10Connections = []

    # Response to incoming http GET request
    def do_GET(self):
        try:
            path = self.path.split('?')[0]
            params = self.path.split('?')[1].split('&')
        
            address = ""
            function = ""
            if path == "/X10/command":
                for param in params:
                   key = param.split('=')[0]
                   value = param.split('=')[1]
                   if key=='address':
                       address = value
                   if key =='function':
                       function = value

                if self.x10Connections != None :
                    for connection in self.x10Connections :
                        connection.HandleCommandFromX10System(address, function)
        
            if path == "/X10/query":
                pass # NOT IMPLEMENTED
                
        except BaseException:
            pass
            # Parse error
#            print "BASE EXCEPTION OCCURED."

        # Send response
        try:
            self.send_response(200)
            self.send_header('Content-type', 'text/html')
            self.end_headers()
            self.wfile.write("OK")
            return
                
        except IOError:
            pass
#            print "RESPONSE ERROR*****"
#            self.send_error(404,'File Not Found')

# The one and only http listener thread for X10 connections
class X10StateListenerThread(Thread):

    # endles server loop
    def run(self):
        self.server = HTTPServer( (self.serverAddress, self.serverPort), HttpRequestHandler)
        s = self.server.fileno()
        print "HTTP SERVER STARTED (X10 Listener): port ",self.serverPort 

        self.stop = False
        self.stopped = False
        while self.stop != True:
            ready = select.select( [s], [], [], 1.0 )
            if s in ready[0]:
                self.server.handle_request()
        self.stopped = True   
        self.server = None 

    def stopServer(self):
        self.stop = True
        while  self.stopped != True:
            time.sleep(0.5)
        print "HTTP SERVER STOPPED (X10 Listener)"    
                

# The connection between X10Actor and the X10Manager server
class X10Connection:

    serverAddress     = "127.0.0.1"
    serverPort        = 30001
    X10ManagerAddress = "127.0.0.1:20000"
    
    httpServerThread = None 

    # param address: the local server address
    # param port:    the local server port 
    def __init__(self):
        if X10Connection.httpServerThread  == None:
            X10Connection.httpServerThread = X10StateListenerThread()
            X10Connection.httpServerThread.serverAddress = X10Connection.serverAddress
            X10Connection.httpServerThread.serverPort = X10Connection.serverPort
            X10Connection.httpServerThread.start()
            
        if HttpRequestHandler.x10Connections.count(self) == 0:     
            HttpRequestHandler.x10Connections.append(self)
        
    def Close(self):
        HttpRequestHandler.x10Connections.remove(self)
        if len( HttpRequestHandler.x10Connections ) == 0:
            X10Connection.httpServerThread.stopServer()
            X10Connection.httpServerThread = None

    def SendX10Command(self, device, command):
        url = 'http://' + X10Connection.X10ManagerAddress + '/X10/command?address=' + device + '&function=' + command
        try:
            sock =  urllib.urlopen(url)
            reponseBody = sock.read()
            sock.close()
        except IOError:
            pass
            
    def HandleCommandFromX10System(self, address, function):
        if self.x10Actor != None:
            self.x10Actor.HandleCommandFromX10System(address, function)
            
    def HandleLocalX10Command(self, address, function):
        if self.x10Actor != None:
            self.x10Actor.HandleCommandFromX10System(address, function)
            
    def EchoLocalX10Message(self, address, function):
        if HttpRequestHandler.x10Connections != None:
            for connection in HttpRequestHandler.x10Connections:
                if connection == self:
                    continue
                connection.HandleLocalX10Command(address, function)
                    
    def SendX10Query(self, device):
        url = 'http://' + self.X10ManagerAddress + '/X10/query?address=' + device 
#        print "X10 QUERY URL: ",url
        try:
            sock =  urllib.urlopen(url).read()
            responseBody = sock.read()
#            print "RESPONSE: " + responseBody
            sock.close()
            lines = responseBody.split('\r')
            if lines[0] == "OK":
                if lines[1] == "On":
                    return "On"
                if lines[1] == "Off":
                    return "Off"    
        except IOError, detail:
#            print "ERROR: CANNOT READ RESPONSE!"
            pass
            # Request failed
            
        return "UNKNOW"


# The X10 actor
# - Mother class for all X10 object classes
class X10Actor(rxactor.Actor):

    @staticmethod
    def GetScriptClassName():
        return "rxX10.X10Actor"
        
    def EventCreated(self):
        try:
            self.InitX10Connection()
        except BaseException:
            pass
            
    def EventDestroyed(self):
        self.x10Connection.Close()

    def InitX10Connection(self):
        self.deviceState = 1
    
        # setup connection
        self.deviceAddress = ""
        self.deviceAddress = self.llGetObjectName()
        self.x10Connection = X10Connection() 
        self.x10Connection.x10Actor = self
        
        # query initial state
        initState = self.x10Connection.SendX10Query( self.deviceAddress )
        if initState == "On":
            self.deviceState = 1
        if initState == "Off":
            self.deviceState = 0

        # Show initial state            
        if self.deviceState == 1:
            self.TurnOnAction()
        else:
            self.TurnOffAction()            

    def HandleCommandFromX10System(self, address, function):
        if address == self.deviceAddress:
            if function == "On" and self.deviceState == 0:
               self.deviceState = 1
               self.TurnOnAction()
            if function == "Off" and self.deviceState == 1:
               self.deviceState = 0
               self.TurnOffAction()

    def TurnOn(self):
        self.deviceState = 1
        self.TurnOnAction()
        self.x10Connection.EchoLocalX10Message(self.deviceAddress, 'On')
        self.x10Connection.SendX10Command(self.deviceAddress, 'On')

    def TurnOff(self):
        self.deviceState = 0
        self.TurnOffAction()
        self.x10Connection.EchoLocalX10Message(self.deviceAddress, 'Off')
        self.x10Connection.SendX10Command(self.deviceAddress, 'Off')

    # Child class should overwrite this 
    def TurnOnAction(self):
        self.llSay(0,"ON")

    # Child class should overwrite this 
    def TurnOffAction(self):
        self.llSay(0,"OFF")

