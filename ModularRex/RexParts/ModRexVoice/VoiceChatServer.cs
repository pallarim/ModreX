using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenMetaverse;
using Nwc.XmlRpc;
using System.Collections;
using ModularRex.RexNetwork;

namespace OpenSim.Region.Communications.VoiceChat
{
    public class VoiceChatServer : IRegionModule
    {
        private static readonly log4net.ILog m_log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        int m_dummySocketPort = 53134;
        int m_voiceServerPort = 12000;
        String m_voiceServerUrl = "";

        Thread m_listenerThread;
        Thread m_mainThread;
        List<Scene> m_scenes = new List<Scene>();
        Socket m_server;
        Socket m_selectCancel;

        Dictionary<Socket, VoiceClient> m_clients;
        Dictionary<UUID, VoiceClient> m_uuidToClient;


        public VoiceChatServer()
        {            
        }

        public void NewClient(IClientAPI client)
        {
            if (client is RexClientView)
            {
                lock (m_uuidToClient)
                {
                    if (!m_uuidToClient.ContainsKey(client.AgentId))
                    {
                        m_log.Info("[VOICECHAT]: New client: " + client.AgentId);
                        m_uuidToClient[client.AgentId] = null;
                    }
                }
            }
        }

        public void RemovePresence(UUID uuid)
        {
            lock (m_uuidToClient)
            {
                if (m_uuidToClient.ContainsKey(uuid))
                {
                    if (m_uuidToClient[uuid] != null)
                    {
                        RemoveClient(m_uuidToClient[uuid].m_socket);
                    }
                    m_uuidToClient.Remove(uuid);
                }
                else
                {
                    m_log.Error("[VOICECHAT]: Presence not found on RemovePresence: " + uuid);
                }
            }
        }

        public bool AddClient(VoiceClient client, UUID uuid)
        { 
            lock (m_uuidToClient)
            {
                if (m_uuidToClient.ContainsKey(uuid))
                {
                    if (m_uuidToClient[uuid] != null) {
                        m_log.Warn("[VOICECHAT]: Multiple login attempts for " + uuid);
                        return false;
                    }
                    m_uuidToClient[uuid] = client;
                    return true;
                } 
            }
            return false;
        }

        public void RemoveClient(Socket socket)
        {
            m_log.Info("[VOICECHAT]: Removing client");
            lock(m_clients)
            {
                VoiceClient client = m_clients[socket];

                lock(m_uuidToClient)
                {
                    if (m_uuidToClient.ContainsKey(client.m_clientId))
                    {
                        m_uuidToClient[client.m_clientId] = null;
                    }
                }

                m_clients.Remove(socket);
                client.m_socket.Close();
            }
        }

        protected void CreateListeningSocket(int voiceServerPort)
        {
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), voiceServerPort);
            m_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_server.Bind(listenEndPoint);
            m_server.Listen(50);
        }

        void ListenIncomingConnections()
        {
            m_log.Info("[VOICECHAT]: Listening connections from port " + m_voiceServerPort.ToString());

            byte[] dummyBuffer = new byte[1];

            while (true)
            {
                try
                {
                    Socket connection = m_server.Accept();
                    lock (m_clients)
                    {
                        m_clients[connection] = new VoiceClient(connection, this);
                        m_selectCancel.Send(dummyBuffer);
                        m_log.Info("[VOICECHAT]: Voicechat connection from " + connection.RemoteEndPoint.ToString());
                    }
                }
                catch (SocketException e)
                {
                    m_log.Error("[VOICECHAT]: During accept: " + e.ToString());
                }
            }
        }

        Socket ListenLoopbackSocket()
        {
            IPEndPoint listenEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), m_dummySocketPort);
            Socket dummyListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            dummyListener.Bind(listenEndPoint);
            dummyListener.Listen(1);
            Socket socket = dummyListener.Accept();
            dummyListener.Close();
            return socket;
        }

        void RunVoiceChat()
        {
            m_log.Info("[VOICECHAT]: Connection handler started...");

            //Listen a loopback socket for aborting select call
            Socket dummySocket = ListenLoopbackSocket();
            
            List<Socket> sockets = new List<Socket>();
            byte[] buffer = new byte[65536];

            while (true)
            {
                if (m_clients.Count == 0)
                {
                    Thread.Sleep(100);
                    continue;
                }

                lock (m_clients)
                {
                    foreach (Socket s in m_clients.Keys)
                    {
                        sockets.Add(s);
                    }
                }
                sockets.Add(dummySocket);

                try
                {
                    Socket.Select(sockets, null, null, 200000);
                }
                catch (SocketException e)
                {
                    m_log.Warn("[VOICECHAT]: " + e.Message);
                }

                foreach (Socket s in sockets)
                {
                    try
                    {
                        if (s.RemoteEndPoint != dummySocket.RemoteEndPoint)
                        {
                            ReceiveFromSocket(s, buffer);
                        }
                        else
                        {
                            //Receive data and check if there was an error with select abort socket
                            if (s.Receive(buffer) <= 0)
                            {
                                //Just give a warning for now
                                m_log.Error("[VOICECHAT]: Select abort socket was closed");
                            }
                        }
                    }
                    catch(ObjectDisposedException)
                    {
                        m_log.Warn("[VOICECHAT]: Connection has been already closed");
                    }
                    catch (Exception e)
                    {
                        m_log.Error("[VOICECHAT]: Exception: " + e.Message);

                        RemoveClient(s);
                    }
                }

                sockets.Clear();
            }
        }

        private void ReceiveFromSocket( Socket s, byte[] buffer )
        {
            int byteCount = s.Receive(buffer);
            if (byteCount <= 0)
            {
                m_log.Info("[VOICECHAT]: Connection lost to " + s.RemoteEndPoint);
                lock (m_clients)
                {
                    RemoveClient(s);
                }
            }
            else
            {
                lock (m_clients)
                {
                    if (m_clients.ContainsKey(s))
                    {
                        m_clients[s].OnDataReceived(buffer, byteCount);
                    }
                    else
                    {
                        m_log.Warn("[VOICECHAT]: Got data from " + s.RemoteEndPoint +
                                   ", but source is not a valid voice client");
                    }
                }
            }
        }

        public void BroadcastVoice(VoicePacket packet)
        {
            Vector3 origPos = new Vector3();// = m_scene.GetScenePresence(packet.m_clientId).AbsolutePosition;
            Scene currentScene = null;
            foreach (Scene s in m_scenes)
            {
                ScenePresence sp = s.GetScenePresence(packet.m_clientId);
                if (sp != null && !sp.IsChildAgent)
                {
                    origPos = sp.AbsolutePosition;
                    currentScene = s;
                    break;
                }
            }
            if (currentScene != null)
            {
                byte[] bytes = packet.GetBytes();
                foreach (VoiceClient client in m_clients.Values)
                {
                    if (client.IsEnabled() && client.m_clientId != packet.m_clientId &&
                        client.m_authenticated && client.IsCodecSupported(packet.m_codec))
                    {
                        ScenePresence presence = currentScene.GetScenePresence(client.m_clientId);

                        if (presence != null && Util.GetDistanceTo(presence.AbsolutePosition, origPos) < 20)
                        {
                            client.SendTo(bytes);
                        }
                    }
                }
            }
            else
            {
                m_log.Warn("[VOICECHAT]: Could not broadcast packet. Packet sender could not be found from registered scenes.");
            }
        }
        
        public XmlRpcResponse XmlRpcVoiceServerAddressRequestHandler(XmlRpcRequest request)
        {
            XmlRpcResponse response = new XmlRpcResponse();
            Hashtable responseValue = new Hashtable();
            responseValue.Add("url", m_voiceServerUrl);
            responseValue.Add("port", m_voiceServerPort);
            response.Value = responseValue;
            return response;
        }

        #region IRegionModule Members

        public void Initialise(Scene scene, Nini.Config.IConfigSource source)
        {
            if (!m_scenes.Contains(scene))
            {
                m_scenes.Add(scene);
            }

            m_voiceServerUrl = source.Configs["realXtend"].GetString("voice_server_url", "");
        }

        public void PostInitialise()
        {
            m_clients = new Dictionary<Socket, VoiceClient>();
            m_uuidToClient = new Dictionary<UUID, VoiceClient>();

            ExctractPortNumberAndValidateUrl(ref m_voiceServerUrl, ref m_voiceServerPort);
            foreach (Scene s in m_scenes)
            {
                s.EventManager.OnNewClient += NewClient;
                s.EventManager.OnRemovePresence += RemovePresence;
                s.CommsManager.HttpServer.AddXmlRPCHandler("voice_chat_server_address_request", this.XmlRpcVoiceServerAddressRequestHandler);
            }
            
            try
            {
                CreateListeningSocket(m_voiceServerPort);
            }
            catch (Exception e)
            {
                m_log.Error("[VOICECHAT]: Unable to start listening", e);
                return;
            }

            m_listenerThread = new Thread(new ThreadStart(ListenIncomingConnections));
            m_listenerThread.IsBackground = true;
            m_listenerThread.Start();

            m_mainThread = new Thread(new ThreadStart(RunVoiceChat));
            m_mainThread.IsBackground = true;
            m_mainThread.Start();

            Thread.Sleep(500);
            m_selectCancel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_selectCancel.Connect("localhost", m_dummySocketPort);
        }

        public void Close()
        {
            foreach (Scene s in m_scenes)
            {
                s.EventManager.OnNewClient -= NewClient;
                s.EventManager.OnRemovePresence -= RemovePresence;
            }
        }

        public string Name
        {
            get { return "RealXtend VoiceChat server"; }
        }

        public bool IsSharedModule
        {
            get { return true; }
        }

        #endregion

        private static void ExctractPortNumberAndValidateUrl(ref String voiceServerUrl, ref int voiceServerPort)
        {
            if (voiceServerUrl.Contains(":"))
            {
                voiceServerPort = Convert.ToInt32(voiceServerUrl.Substring(voiceServerUrl.LastIndexOf(":") + 1).Replace("/", ""));


                voiceServerUrl = voiceServerUrl.Substring(0, voiceServerUrl.LastIndexOf(":"));
            }

            if (voiceServerUrl.StartsWith("http://") && voiceServerUrl.Length > 7)
            {
                voiceServerUrl = voiceServerUrl.Substring(7);
            }

            IPAddress IP;
            if (!IPAddress.TryParse(voiceServerUrl, out IP))
            {
                try
                {
                    IPHostEntry iph = Dns.GetHostEntry(voiceServerUrl);

                    if (iph.AddressList.Length < 1)
                    {
                        voiceServerUrl = "";
                    }
                }
                catch (Exception e)
                {
                    m_log.Error("[VOICECHAT]: Voice server url not valid, please check.", e);
                }
            }
            if (voiceServerUrl != "")
            {
                voiceServerUrl = "http://" + voiceServerUrl;
            }

            if (voiceServerPort < 0 || voiceServerPort > 65535)
            {
                m_log.Error("[VOICECHAT]: Voice server port number not valid or incorrect! Please check your opensim.ini -> voice_server_url. Using default port (12000).");

                voiceServerPort = 12000;
            }
        }
    }
}
