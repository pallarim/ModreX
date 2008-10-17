using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Nini.Config;
using OpenSim.Region.ClientStack;
using OpenSim.Region.Environment.Interfaces;
using OpenSim.Region.Environment.Scenes;

namespace ModularRex.RexNetwork.RexLogin
{
    class RexLoginModule : IRegionModule 
    {
        private Scene m_firstScene;
        private IClientNetworkServer m_udpserver;

        public void Initialise(Scene scene, IConfigSource source)
        {
            if (m_udpserver == null)
                m_udpserver = new RexUDPServer();

            m_udpserver.AddScene(scene);

            m_firstScene = scene;
            scene.AddHTTPHandler("/rexlogin", OnLoginRequest);
        }

        public Hashtable OnLoginRequest(Hashtable keysvals)
        {
            Hashtable reply = new Hashtable();
            int statuscode = 200;

            reply["str_response_string"] = "Test";
            reply["int_response_code"] = statuscode;
            reply["content_type"] = "text/xml";

            return reply;
        }


        public void PostInitialise()
        {
        }

        public void Close()
        {
            
        }

        public string Name
        {
            get { return "RexLoginOverrider"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }
    }
}
