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
        private List<IWebDAVResource> m_rootFolders = new List<IWebDAVResource>();

        public WorldInventoryServer()
        {
            AddRootFolders();
        }

        public WorldInventoryServer(List<Scene> scenes) : this()
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

        private void AddRootFolders()
        {
            //only four folders now, add when need more
            m_rootFolders.Add(new WebDAVFolder("/inventory/3d models", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            m_rootFolders.Add(new WebDAVFolder("/inventory/3d animations", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            m_rootFolders.Add(new WebDAVFolder("/inventory/materials", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            m_rootFolders.Add(new WebDAVFolder("/inventory/textures", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            m_rootFolders.Add(new WebDAVFolder("/inventory/sounds", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
            m_rootFolders.Add(new WebDAVFolder("/inventory/particles", DateTime.MinValue, DateTime.MinValue, DateTime.MinValue, false));
        }

        public void Stop()
        {
            m_webdav.OnPropFind -= PropFindHandler;
            m_listener.Stop();
        }

        IList<IWebDAVResource> PropFindHandler(string username, string path, DepthHeader depth)
        {
            string[] pathParts = path.Split('/');
            if (pathParts.Length >= 2 && pathParts[1] == "inventory")
            {
                if (pathParts.Length == 2 || (pathParts.Length == 3 && pathParts[2] == String.Empty))
                {
                    return m_rootFolders;
                }
                else
                {
                    //find the sub resource and return it's properties
                }
            }

            return null;
        }
    }
}
