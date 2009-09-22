using System;
using System.Collections.Generic;
using System.Text;
using HttpServer;
using WebDAVSharp;
using OpenSim.Region.Framework.Scenes;

namespace ModularRex.WorldInventory
{
    public class WorldInventoryServer
    {
        private HttpServerLogWriter httpserverlog = new HttpServerLogWriter();

        protected HttpListener m_listener = null;
        protected WebDAVListener m_webdav = null;

        private List<Scene> m_scenes = null;

        public WorldInventoryServer()
        {
        }

        public WorldInventoryServer(List<Scene> scenes)
        {
            m_scenes = scenes;
        }

        public void Start(System.Net.IPAddress ip, int port)
        {
            m_listener = HttpListener.Create(httpserverlog, ip, port);
            m_webdav = new WebDAVListener(m_listener, @"^/inventory/");
            m_listener.Start(10);

            m_webdav.OnPropFind += PropFindHandler;
        }

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_listener.Stop();
        }

        IList<IWebDAVResource> PropFindHandler(string username, string path, DepthHeader depth)
        {
            return null;
        }
    }
}
