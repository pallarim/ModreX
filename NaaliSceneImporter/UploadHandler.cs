using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Xml.Serialization;
using System.Xml;

using OpenSim.Region.Framework.Scenes;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Framework;

using OpenMetaverse;
using log4net;

using ModularRex.RexFramework;

namespace NaaliSceneImporter
{
    public delegate byte[] HttpRequestCallback(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse);

    public class StreamHandler : BaseStreamHandler
    {
        private HttpRequestCallback m_callback;

        public override string ContentType { get { return null; } }

        public StreamHandler(string httpMethod, string path, HttpRequestCallback callback) :
            base(httpMethod, path)
        {
            m_callback = callback;
        }

        public override byte[] Handle(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            return m_callback(path, request, httpRequest, httpResponse);
        }
    }

    class RegisterCaps
    {
        private Scene m_scene;
        private NaaliSceneImportModule m_nsi;
        protected Dictionary<UUID, UploadHandler> m_capsHandlers = new Dictionary<UUID, UploadHandler>();

        public RegisterCaps(Scene scene, NaaliSceneImportModule nsi)
        {
            m_scene = scene;
            m_nsi = nsi;
            m_scene.EventManager.OnRegisterCaps += OnAgentRegisterCaps;
        }

        public void OnAgentRegisterCaps(UUID agentID, OpenSim.Framework.Capabilities.Caps caps)
        {
            if (CheckRights(agentID))
            {
                m_capsHandlers[agentID] = new UploadHandler(caps, m_scene, agentID, m_nsi);
            }
        }

        private bool CheckRights(UUID agentID)
        {
            if (!m_scene.Permissions.BypassPermissions())
            {
                if (agentID == m_scene.RegionInfo.EstateSettings.EstateOwner)
                    return true;
                UUID[] managers = m_scene.RegionInfo.EstateSettings.EstateManagers;
                foreach (UUID id in managers)
                {
                    if (id == agentID)
                        return true;
                }
            }
            else 
            {
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Handle adding caps handlers for uploading scenes, when user, with rights to upload scene files, logs in,
    /// + handle uploads
    /// </summary>
    public class UploadHandler
    {
        /// <summary>
        /// Serializable class to help returning SceneName, RegionId and SceneUUID as response to GetUploadSceneList request
        /// </summary>
        [Serializable()]
        public class SceneRegion
        {
            public string SceneName;
            public string Region;
            public string SceneUuid;
        }

        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene;
        private UUID m_agentId = UUID.Zero;
        
        private NaaliSceneImportModule m_nsi;

        public UploadHandler(OpenSim.Framework.Capabilities.Caps caps, Scene scene, UUID agentId, NaaliSceneImportModule nsi)
        {
            m_scene = scene;
            m_agentId = agentId;
            m_nsi = nsi;

            UUID capID = UUID.Random();
            m_log.InfoFormat("[NAALISCENE]: Creating capability: /CAPS/{0}", capID);
            caps.RegisterHandler("UploadNaaliScene", new StreamHandler("POST", "/CAPS/" + capID, ProcessUploadSceneMessages));
        }

        private byte[] ProcessUploadSceneMessages(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            string method = httpRequest.Headers["ImportMethod"];

            // Dispatch
            switch (method)
            {
                case "Upload": 
                    return ProcessUploadScene(path, request, httpRequest, httpResponse);
                default:
                    httpResponse.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    httpResponse.StatusDescription = "Method '" + method + "' not allowed";
                    return Utils.EmptyBytes;
            }
        }

        private byte[] ProcessUploadScene(string path, Stream request, OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            byte[] data = httpRequest.GetBody();

            string region_x = httpRequest.Headers["RegionX"];
            string region_y = httpRequest.Headers["RegionY"];

            Scene scene = null;
            if (region_x != null && region_y != null)
            {
                scene = GetScene(region_x, region_y);
                if (scene == null)
                {
                    m_log.ErrorFormat("[NAALISCENE]: Could not process upload request, region in location ({0},{1}) not found.", region_x.ToString(), region_y.ToString());
                    httpResponse.StatusCode = (int)HttpStatusCode.NotAcceptable;
                    httpResponse.StatusDescription = "Region with input parameters was not found";
                }
                else
                {
                    try
                    {
                        m_log.Info("[NAALISCENE]: Processing HTTP POST XML import");
                        m_log.InfoFormat("[NAALISCENE]: >> Import region: {0} located at ({1},{2})", scene.RegionInfo.RegionName, region_x.ToString(), region_y.ToString());
                        m_nsi.ImportNaaliScene(data, scene);
                        httpResponse.StatusCode = (int)HttpStatusCode.OK;
                        httpResponse.StatusDescription = "Scene uploaded and instantiated succesfully";
                        m_log.Info("[NAALISCENE]: Scene imported and instantiated succesfully");
                    }
                    catch (Exception e)
                    {
                        m_log.ErrorFormat("[NAALISCENE]: Exception occurred while processing uploaded data. {0}", e);
                        httpResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                        httpResponse.StatusDescription = "Exception occurred while processing uploaded data";
                    }
                }
            }
            else
            {
                m_log.Error("[NAALISCENE]: Could not process upload request, 'RegionX' and 'RegionY' headers not spesified.");
                httpResponse.StatusCode = (int)HttpStatusCode.NotAcceptable;
                httpResponse.StatusDescription = "RegionX and RegionY headers not spesified";
            }
            return Utils.EmptyBytes;
        }

        private Scene GetScene(string region_x, string region_y)
        {
            uint x = System.Convert.ToUInt32(region_x);
            uint y = System.Convert.ToUInt32(region_y);

            //OpenSim.Services.Interfaces.GridRegion gridRegion = m_scene.GridService.GetRegionByName(UUID.Zero, region);
            // dont know how to get scene handle so asking scenes OgreSceneImportModule, if there's some smarter way of doing these feel free to fix this
            foreach (Scene s in m_nsi.GetScenes())
            {
                if (s.RegionInfo.RegionLocX == x && s.RegionInfo.RegionLocY == y)
                    return s;
            }
            return null;
        }
    }
}
